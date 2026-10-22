using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 12, E14: full proofs for the Fixed shapes of rows 8 and 9 (styles, layout). Each
// Proofs/EPPlus/<Row>/_Combined/<case>.before.cs runs through the real EPPlusFix (all patterns, real order); <case>.after.cs is its
// exact output, which must compile against ClosedXML alone. EPPlusHarnessProofTests runs every such after.cs through the cell-compare
// harness against its before.cs (E1). ProofTests skips _Combined folders.
public class StylesCombinedProofTests
{
    private static readonly string s_proofsDirectory = Path.Combine(AppContext.BaseDirectory, "Proofs", "EPPlus");

    // Dimension and LoadFrom follow once epplus.package maps a sheet or range that those rows rewrote (claim request).
    private static readonly string[] s_rows = ["Styles", "Layout"];

    public static TheoryData<string> Cases() =>
        [.. s_rows.SelectMany(row => Directory.EnumerateFiles(Path.Combine(s_proofsDirectory, row, "_Combined"), "*.before.cs"))
            .Select(p => Path.GetRelativePath(s_proofsDirectory, p)[..^".before.cs".Length].Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Combined_Before_BecomesAfter(string proof)
    {
        // Arrange
        string basePath = Path.Combine(s_proofsDirectory, proof);
        string before = Normalize(await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken));
        TestSolution.CompileErrors(before, EPPlusReferences.EPPlus).ShouldBeEmpty("before must compile against EPPlus");
        Solution solution = TestSolution.Create(before, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        report.Findings.ShouldAllBe(f => f.Result is FixedResult || f.Result is InfoResult,
            string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string afterPath = basePath + ".after.cs";
        File.Exists(afterPath).ShouldBeTrue($"no {proof}.after.cs; the Fixer wrote:\n{actual}");
        actual.ShouldBe(Normalize(await File.ReadAllTextAsync(afterPath, TestContext.Current.CancellationToken)));
        TestSolution.CompileErrors(actual, EPPlusReferences.ClosedXml).ShouldBeEmpty("after must compile against ClosedXML");
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");
}
