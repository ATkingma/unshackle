using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

// Council round 8, general rule: a Fixed shape needs a proof that pins the behaviour that could change. Each Fixed proof in
// Proofs/Polly/WaitAndRetryAsync runs before (Polly v7) and after (Polly.Core 8); Proof.RunAsync returns a trace of attempts,
// delay arguments and the outcome, and both traces must be equal.
public class PollyBehaviourProofTests
{
    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "Polly", "WaitAndRetryAsync");

    [Theory]
    [InlineData("local-block-delay", "delay 1, delay 2, delay 3, caught call 4, calls 4")]
    [InlineData("inline-result", "call 1, call 2, call 3, result 30")]
    [InlineData("typed-local-not-handled", "caught call 1, calls 1")]
    public async Task Proof_BeforeAndAfter_SameTrace(string proof, string expected)
    {
        // Arrange
        string before = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".before.cs"), TestContext.Current.CancellationToken);
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".after.cs"), TestContext.Current.CancellationToken);

        // Act
        string beforeTrace = await RunAsync(before, PollyReferences.Before);
        string afterTrace = await RunAsync(after, PollyReferences.After);

        // Assert
        beforeTrace.ShouldBe(expected);
        afterTrace.ShouldBe(beforeTrace);
    }

    [Fact]
    public async Task Proof_WithoutAttemptOffset_DifferentTrace()
    {
        // Arrange: the proof must fail when the 0-based AttemptNumber is passed on as is.
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, "local-block-delay.after.cs"), TestContext.Current.CancellationToken);
        string mutated = after.Replace("args.AttemptNumber + 1", "args.AttemptNumber", StringComparison.Ordinal);

        // Act
        string trace = await RunAsync(mutated, PollyReferences.After);

        // Assert
        trace.ShouldBe("delay 0, delay 1, delay 2, caught call 4, calls 4");
    }

    private static async Task<string> RunAsync(string code, IEnumerable<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create(
            "PollyProof" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue(string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        var assembly = Assembly.Load(stream.ToArray());
        MethodInfo run = assembly.GetType("Proof")!.GetMethod("RunAsync")!;
        return await (Task<string>)run.Invoke(null, null)!;
    }
}
