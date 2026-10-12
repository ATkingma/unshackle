using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests;

// Runs Proofs/FluentAssertions/_Combined/<case>.before.cs through the real FluentAssertionsFix (all patterns, real order).
// Expected: <case>.after.cs (exact text). Left-over FluentAssertions code (Manual findings) must still compile, so then
// "after" is compiled against Shouldly and FluentAssertions.
// Proofs/AutoMapper/_Combined runs the real AutoMapperFix: a Map call only becomes a.ToB() next to the profile rewrite that writes ToB.
public class CombinedProofTests
{
    public const string FolderName = "_Combined";

    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "FluentAssertions", FolderName);

    private static readonly string s_autoMapperDirectory = Path.Combine(AppContext.BaseDirectory, "Proofs", "AutoMapper", FolderName);

    public static TheoryData<string> Cases() =>
        [.. Directory.EnumerateFiles(s_directory, "*.before.cs")
            .Select(p => Path.GetFileName(p)[..^".before.cs".Length])
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Combined_Before_BecomesAfter(string name)
    {
        // Arrange
        string basePath = Path.Combine(s_directory, name);
        string before = Normalize(await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken));
        string expected = Normalize(await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken));
        IEnumerable<MetadataReference> afterReferences = expected.Contains("using FluentAssertions;", StringComparison.Ordinal)
            ? TestReferences.FluentAssertions.Concat(TestReferences.Shouldly).DistinctBy(r => r.Display)
            : TestReferences.Shouldly;
        TestSolution.CompileErrors(before, TestReferences.FluentAssertions).ShouldBeEmpty("before must compile against FluentAssertions");
        var fixer = new Fixer([new FluentAssertionsFix(TestReferences.ShouldlyAssembly)]);
        Solution solution = TestSolution.Create(before, TestReferences.FluentAssertions);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "fluentassertions" }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        actual.ShouldBe(expected);
        TestSolution.CompileErrors(actual, afterReferences).ShouldBeEmpty("after must compile");
    }

    public static TheoryData<string> AutoMapperCases() =>
        [.. Directory.EnumerateFiles(s_autoMapperDirectory, "*.before.cs")
            .Select(p => Path.GetFileName(p)[..^".before.cs".Length])
            .Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(AutoMapperCases))]
    public async Task CombinedAutoMapper_Before_BecomesAfter(string name)
    {
        // Arrange
        string basePath = Path.Combine(s_autoMapperDirectory, name);
        string before = Normalize(await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken));
        string expected = Normalize(await File.ReadAllTextAsync(basePath + ".after.cs", TestContext.Current.CancellationToken));
        TestSolution.CompileErrors(before, TestReferences.AutoMapper).ShouldBeEmpty("before must compile against AutoMapper");
        var fixer = new Fixer([new AutoMapperFix()]);
        Solution solution = TestSolution.Create(before, TestReferences.AutoMapper);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "automapper" }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        actual.ShouldBe(expected);
        TestSolution.CompileErrors(actual, TestReferences.AutoMapper).ShouldBeEmpty("after must compile");
    }

    [Fact]
    public void AllSatisfyPattern_IsFirstAfterAssertionScope()
    {
        // Arrange
        var fix = new FluentAssertionsFix();

        // Act
        string[] first = [.. fix.Patterns.Take(2).Select(p => p.Id)];

        // Assert
        first.ShouldBe(["fa.assertion-scope", "fa.all-satisfy"], "AllSatisfy copies its lambda body into a foreach with FluentAssertions code still in it; "
            + "the patterns after it rewrite those statements. Placed later, the loop body would stay FluentAssertions code.");
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");
}
