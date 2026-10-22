using Microsoft.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 12, E14: Proofs/EPPlus/_Combined/<case>.before.cs runs through the real EPPlusFix (all patterns, real order, a non-packable
// project with no IsWorksheets1Based in config). The result must equal <case>.after.cs exactly, compile against ClosedXML, and (through
// EPPlusHarnessProofTests) write the same workbook. A missing .after.cs is written from the actual output and the test fails, for review.
public class EPPlusCombinedProofTests
{
    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "EPPlus", "_Combined");

    public static TheoryData<string> Cases() =>
        [.. Directory.EnumerateFiles(s_directory, "*.before.cs").Select(p => Path.GetFileName(p)[..^".before.cs".Length]).Order(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Combined_Before_BecomesAfter(string name)
    {
        // Arrange
        string basePath = Path.Combine(s_directory, name);
        string before = (await File.ReadAllTextAsync(basePath + ".before.cs", TestContext.Current.CancellationToken)).ReplaceLineEndings("\n");
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.IsPackable(Arg.Any<string>()).Returns(false);
        IConfigKeySearch search = Substitute.For<IConfigKeySearch>();
        search.Find(Arg.Any<string>(), Arg.Any<string>()).Returns([]);
        var fixer = new Fixer([new EPPlusFix(properties, search)]);
        Solution solution = TestSolution.Create(before, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        string actual = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string afterPath = basePath + ".after.cs";
        if (!File.Exists(afterPath))
        {
            string source = Path.Combine(RepositoryRoot.Path, "src", "Unshackle.Tests", "Proofs", "EPPlus", "_Combined", name + ".after.cs");
            await File.WriteAllTextAsync(source, actual, TestContext.Current.CancellationToken);
            Assert.Fail($"{name}.after.cs was missing and is now written from the actual output; review it.");
        }

        actual.ShouldBe((await File.ReadAllTextAsync(afterPath, TestContext.Current.CancellationToken)).ReplaceLineEndings("\n"));
        TestSolution.CompileErrors(actual, EPPlusReferences.ClosedXml).ShouldBeEmpty();
    }
}
