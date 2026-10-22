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

// Council round 8, E1 for queue row 8 (epplus.styles): every Fixed style shape is run through the cell-compare harness. The style
// statement is rewritten by StylesPattern itself; the scaffold around it (package, sheet, cell, save: rows 2-4, not built yet) is
// translated by hand here, so the harness compares exactly the pattern's output. Each case also fails when the style is wrong.
public class StylesHarnessTests
{
    private const string Before = """
        using System.Drawing;
        using System.IO;
        using OfficeOpenXml;
        using OfficeOpenXml.Style;

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new ExcelPackage();
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("S");
                sheet.Cells["A1"].Value = "x";
        STYLE
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    // Style lines (before), and a change of the rewritten text that makes the style wrong (the harness must see it).
    public static TheoryData<string, string, string> Shapes() => new()
    {
        { "sheet.Cells[\"A1\"].Style.Font.Bold = true;", "Bold = true", "Bold = false" },
        { "sheet.Cells[\"A1\"].Style.Font.Italic = true;", "Italic = true", "Italic = false" },
        { "sheet.Cells[\"A1\"].Style.Font.Strike = true;", "Strikethrough = true", "Strikethrough = false" },
        { "sheet.Cells[\"A1\"].Style.Font.Size = 14;", "FontSize = 14", "FontSize = 15" },
        { "sheet.Cells[\"A1\"].Style.Font.Name = \"Courier New\";", "Courier New", "Arial" },
        { "sheet.Cells[\"A1\"].Style.Font.Color.SetColor(Color.Red);", "Color.Red", "Color.Blue" },
        {
            "sheet.Cells[\"A1\"].Style.Fill.PatternType = ExcelFillStyle.Solid;\n        sheet.Cells[\"A1\"].Style.Fill.BackgroundColor.SetColor(Color.Yellow);",
            "Color.Yellow", "Color.Green"
        },
        { "sheet.Cells[\"A1\"].Style.Numberformat.Format = \"0.000\";", "\"0.000\"", "\"0.00\"" },
        { "sheet.Cells[\"A1\"].Style.WrapText = true;", "WrapText = true", "WrapText = false" },
        { "sheet.Cells[\"A1\"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;", "Values.Center", "Values.Right" },
        { "sheet.Cells[\"A1\"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;", "Values.Top", "Values.Bottom" },
        { "sheet.Cells[\"A1\"].Style.Border.Top.Style = ExcelBorderStyle.Thin;", "TopBorder = XLBorderStyleValues.Thin", "TopBorder = XLBorderStyleValues.Thick" },
        { "sheet.Cells[\"A1\"].Style.Border.Bottom.Style = ExcelBorderStyle.Double;", "Values.Double", "Values.Thin" },
        { "sheet.Cells[\"A1\"].Style.Border.Left.Style = ExcelBorderStyle.Dashed;", "LeftBorder", "RightBorder" },
        { "sheet.Cells[\"A1\"].Style.Border.Right.Style = ExcelBorderStyle.Medium;", "Values.Medium", "Values.Hair" },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task StyleShape_RewrittenByPattern_WritesSameWorkbook(string style, string right, string wrong)
    {
        // Arrange
        string before = Before.Replace("STYLE", "        " + style, StringComparison.Ordinal);

        // Act
        (FixReport report, string after) = await RewriteAsync(before);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result.ToString())));
        after.ShouldContain(right);
        CellCompare.Differences(before, after).ShouldBeEmpty();
        CellCompare.Differences(before, after.Replace(right, wrong, StringComparison.Ordinal)).ShouldNotBeEmpty("a wrong style must show in the harness");
    }

    [Fact]
    public async Task FillPatternWithoutColor_IsManual()
    {
        // Arrange
        string before = Before.Replace("STYLE", "        sheet.Cells[\"A1\"].Style.Fill.PatternType = ExcelFillStyle.Solid;", StringComparison.Ordinal);

        // Act
        (FixReport report, string after) = await RewriteAsync(before, translate: false);

        // Assert
        ((ManualResult)report.Findings.ShouldHaveSingleItem().Result).Reason.ShouldContain(StylesPattern.FillCode);
        after.ShouldBe(before.ReplaceLineEndings("\n"));
    }

    // Runs StylesPattern alone (like ProofTests) and translates the scaffold of rows 2-4 by hand.
    private static async Task<(FixReport Report, string After)> RewriteAsync(string before, bool translate = true)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("epplus");
        fix.Patterns.Returns([new StylesPattern()]);
        fix.RemovedNamespaces.Returns(["OfficeOpenXml"]);
        fix.AddedNamespaces.Returns([]);
        Solution solution = TestSolution.Create(before.ReplaceLineEndings("\n"), EPPlusReferences.EPPlus);
        FixReport report = await new Fixer([fix]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        if (!translate)
        {
            return (report, text);
        }

        string after = text
            .Replace("using OfficeOpenXml.Style;\n", string.Empty, StringComparison.Ordinal)
            .Replace("using OfficeOpenXml;\n", text.Contains("using ClosedXML.Excel;", StringComparison.Ordinal) ? string.Empty : "using ClosedXML.Excel;\n", StringComparison.Ordinal)
            .Replace("new ExcelPackage()", "new XLWorkbook()", StringComparison.Ordinal)
            .Replace("ExcelWorksheet sheet = package.Workbook.Worksheets.Add(\"S\");", "IXLWorksheet sheet = package.Worksheets.Add(\"S\");", StringComparison.Ordinal)
            .Replace("sheet.Cells[\"A1\"]", "sheet.Cell(\"A1\")", StringComparison.Ordinal)
            .Replace("package.SaveAs(new FileInfo(path));", "package.SaveAs(path);", StringComparison.Ordinal);
        return (report, after);
    }
}
