using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

// Council round 8, general rule: a Fixed shape needs a proof that pins the behaviour that could change. Each Fixed proof in
// Proofs/Polly/<Pattern> runs before (Polly v7) and after (Polly.Core 8); Proof.RunAsync returns a trace of attempts,
// delay arguments and the outcome, and both traces must be equal.
public class PollyBehaviourProofTests
{
    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "Polly");

    [Theory]
    [InlineData("WaitAndRetryAsync/local-block-delay", "delay 1, delay 2, delay 3, caught call 4, calls 4")]
    [InlineData("WaitAndRetryAsync/inline-result", "call 1, call 2, call 3, result 30")]
    [InlineData("WaitAndRetryAsync/typed-local-not-handled", "caught call 1, calls 1")]
    [InlineData("WaitAndRetryAsync/filter-or-predicate", "throw t, throw go, throw stop, caught InvalidOperationException stop")]
    [InlineData("WaitAndRetryAsync/filter-handle-result", "got -1, got 0, got 5, result 5, got -2, got -3, got -4, result -4")]
    [InlineData("WaitAndRetryAsync/filter-policy-of-t", "result ok left 0")]
    [InlineData("WaitAndRetryAsync/on-retry-attempt", "delay 1, retry 1 after 1 call 1, delay 2, retry 2 after 2 call 2, caught call 3")]
    [InlineData("WaitAndRetryAsync/on-retry-async", "retry after call 1, retry after call 2, result 3")]
    [InlineData("WaitAndRetryAsync/field-static", "call 1, call 2, fail, fail, fail, caught always")]
    [InlineData("RetryVariants/field-instance", "first 3, second -6, left 0")]
    [InlineData("RetryVariants/retry-count", "retry 1 call 1, retry 2 call 2, caught call 3")]
    [InlineData("RetryVariants/retry-default", "call 1, call 2, caught call 2")]
    [InlineData("RetryVariants/wait-forever", "delay 1, delay 2, delay 3, delay 4, result 5 calls 9")]
    [InlineData("RetryVariants/durations", "wait 0, wait 1, wait 2, caught call 4")]
    [InlineData("WaitAndRetryAsync/on-retry-result", "retry -1 none, retry 0 TimeoutException, result 7")]
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
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, "WaitAndRetryAsync", "local-block-delay.after.cs"), TestContext.Current.CancellationToken);
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
