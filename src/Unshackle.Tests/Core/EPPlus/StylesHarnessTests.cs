using Microsoft.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 8 E1 and round 12 E14 for queue row 8 (epplus.styles). The full proofs in Proofs/EPPlus/Styles/_Combined are the Fixer's
// real output (StylesCombinedProofTests) and compare equal in the harness (EPPlusHarnessProofTests). This test shows the harness would
// see a wrong style: each proof's after.cs, with its style changed, must differ from its before.cs.
public class StylesHarnessTests
{
    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "EPPlus", "Styles", "_Combined");

    // Proof, the rewritten text, and a change of it that makes the style wrong.
    public static TheoryData<string, string, string> Shapes() => new()
    {
        { "font-bold", "Bold = true", "Bold = false" },
        { "font-italic", "Italic = true", "Italic = false" },
        { "font-strike", "Strikethrough = true", "Strikethrough = false" },
        { "font-size", "FontSize = 14", "FontSize = 15" },
        { "font-name", "Courier New", "Arial" },
        { "font-color", "Color.Red", "Color.Blue" },
        { "fill-solid", "Color.Yellow", "Color.Green" },
        { "number-format", "\"0.000\"", "\"0.00\"" },
        { "wrap-text", "WrapText = true", "WrapText = false" },
        { "horizontal", "Values.Center", "Values.Right" },
        { "vertical", "Values.Top", "Values.Bottom" },
        { "border-top", "TopBorder = XLBorderStyleValues.Thin", "TopBorder = XLBorderStyleValues.Thick" },
        { "border-bottom", "Values.Double", "Values.Thin" },
        { "border-left", "LeftBorder", "RightBorder" },
        { "border-right", "Values.Medium", "Values.Hair" },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task StyleProof_WrongStyle_IsSeenByHarness(string proof, string right, string wrong)
    {
        // Arrange
        string before = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".before.cs"), TestContext.Current.CancellationToken);
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".after.cs"), TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after.Replace(right, wrong, StringComparison.Ordinal));

        // Assert
        after.ShouldContain(right);
        differences.ShouldNotBeEmpty("a wrong style must show in the harness");
    }

    [Fact]
    public async Task FillPatternWithoutColor_IsManual()
    {
        // Arrange
        const string code = """
            using OfficeOpenXml;
            using OfficeOpenXml.Style;

            internal static class Report
            {
                internal static void Format(ExcelRange cell)
                {
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                }
            }
            """;
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("epplus");
        fix.Patterns.Returns([new StylesPattern()]);
        fix.RemovedNamespaces.Returns(["OfficeOpenXml"]);
        fix.AddedNamespaces.Returns([]);
        Solution solution = TestSolution.Create(code, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await new Fixer([fix]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        ((ManualResult)report.Findings.ShouldHaveSingleItem().Result).Code.ShouldBe(nameof(Unshackle.Core.Fixes.EPPlus.EPPlusReason.FillWithoutSolidPattern));
        report.Solution.ShouldBeSameAs(solution);
    }
}
