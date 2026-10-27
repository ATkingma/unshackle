using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

// Run-time proofs (council round 9, Q2): Proofs/Moq/_Runtime/<case>.cs is test code on Moq with a consumer and a
// `public static class Probe { public static string Run() }`. The real MoqFix rewrites it; "before" runs on Moq, "after" on
// NSubstitute, and both must give the same outcome: the returned text, or the kind of exception (a failed Verify/Received is
// "VerifyFailed" in both). A first line `// Expect: <outcome>` pins the outcome too, so a case that must fail under both cannot pass vacuously.
// Every finding must be Fixed, and the after text must not use Moq anymore.
public class MoqRuntimeProofTests
{
    private const string ExpectPrefix = "// Expect: ";

    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "Moq", "_Runtime");

    public static TheoryData<string> Cases() =>
        [.. Directory.EnumerateFiles(s_directory, "*.cs").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).Cast<string>()];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Runtime_BeforeOnMoqAndAfterOnNSubstitute_SameOutcome(string name)
    {
        // Arrange
        string before = (await File.ReadAllTextAsync(Path.Combine(s_directory, name + ".cs"), TestContext.Current.CancellationToken)).ReplaceLineEndings("\n");
        var fixer = new Fixer([new MoqFix(MoqReferences.NSubstituteAssembly)]);
        Solution solution = TestSolution.Create(before, MoqReferences.Moq);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "moq" }, TestContext.Current.CancellationToken);
        string after = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        string beforeOutcome = Run(name + "_before", before, MoqReferences.Moq);
        string afterOutcome = Run(name + "_after", after, MoqReferences.NSubstitute);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult || f.PatternId == Fixer.ChangedCodeInfoPatternId, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        after.ShouldNotContain("using Moq;", customMessage: after);
        afterOutcome.ShouldBe(beforeOutcome, after);
        if (before.StartsWith(ExpectPrefix, StringComparison.Ordinal))
        {
            beforeOutcome.ShouldBe(before[ExpectPrefix.Length..before.IndexOf('\n', StringComparison.Ordinal)].Trim());
        }
    }

    private static string Run(string assemblyName, string code, IEnumerable<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName, [CSharpSyntaxTree.ParseText(code)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        EmitResult emitted = compilation.Emit(stream);
        emitted.Success.ShouldBeTrue(string.Join("\n", emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) + "\n" + code);
        stream.Position = 0;

        // Not collectible: Castle's and NSubstitute's dynamic proxies cannot reference a collectible assembly.
        Assembly assembly = new AssemblyLoadContext(assemblyName + Guid.NewGuid().ToString("N")).LoadFromStream(stream);
        MethodInfo probe = assembly.GetTypes().Single(t => t.Name == "Probe").GetMethod("Run")!;
        try
        {
            return (string)probe.Invoke(null, null)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
        {
            return inner.GetType().FullName switch
            {
                "Moq.MockException" or "NSubstitute.Exceptions.ReceivedCallsException" => "VerifyFailed",
                string type => "Threw " + type,
                _ => "Threw",
            };
        }
    }
}
