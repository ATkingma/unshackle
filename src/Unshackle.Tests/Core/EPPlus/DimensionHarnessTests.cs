using System.Text.RegularExpressions;
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

// Council round 8, E1 for queue row 7 (epplus.dimension): the rewritten expression's result is written to a second sheet, so the
// cell-compare harness compares it. The scaffold (rows 2-5) is translated by hand, as for styles and layout. Each case also fails when
// the expression is wrong; an empty sheet keeps null (Dimension?. → ?.).
public partial class DimensionHarnessTests
{
    private const string Before = """
        using System.IO;
        using OfficeOpenXml;

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new ExcelPackage();
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("S");
                ExcelWorksheet result = package.Workbook.Worksheets.Add("Result");
        SETUP
                int? found = EXPRESSION;
                result.Cells["A1"].Value = found ?? -1;
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    private const string Cells = "sheet.Cells[\"B2\"].Value = 1; sheet.Cells[\"D5\"].Value = 2;";

    public static TheoryData<string, string, string, string> Shapes() => new()
    {
        { Cells, "sheet.Dimension.End.Row", "LastRowUsed", "FirstRowUsed" },
        { Cells, "sheet.Dimension.End.Column", "LastColumnUsed", "FirstColumnUsed" },
        { Cells, "sheet.Dimension.Start.Row", "FirstRowUsed", "LastRowUsed" },
        { Cells, "sheet.Dimension.Start.Column", "FirstColumnUsed", "LastColumnUsed" },
        { Cells, "sheet.Dimension?.End.Row", "LastRowUsed()?.RowNumber()", "LastColumnUsed()?.ColumnNumber()" },
        { string.Empty, "sheet.Dimension?.End.Row", "found ?? -1", "found ?? -2" },
        { string.Empty, "sheet.Dimension?.Start.Column", "FirstColumnUsed()?.ColumnNumber()", "FirstColumnUsed()?.ColumnNumber() ?? 0" },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task DimensionShape_RewrittenByPattern_WritesSameWorkbook(string setup, string expression, string right, string wrong)
    {
        // Arrange
        string before = Before.Replace("SETUP", "        " + setup, StringComparison.Ordinal).Replace("EXPRESSION", expression, StringComparison.Ordinal);

        // Act
        (FixReport report, string after) = await RewriteAsync(before);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<FixedResult>();
        after.ShouldContain(right);
        CellCompare.Differences(before, after).ShouldBeEmpty();
        CellCompare.Differences(before, after.Replace(right, wrong, StringComparison.Ordinal)).ShouldNotBeEmpty("a wrong expression must show in the harness");
    }

    [Theory]
    [InlineData("string address = sheet.Dimension.Address;")]
    [InlineData("bool empty = sheet.Dimension == null;")]
    [InlineData("int rows = sheet.Dimension.Rows;")]
    public async Task OtherDimensionUse_IsManual(string statement)
    {
        // Arrange
        string before = Before.Replace("SETUP", "        " + statement, StringComparison.Ordinal).Replace("EXPRESSION", "1", StringComparison.Ordinal);

        // Act
        (FixReport report, _) = await RewriteAsync(before);

        // Assert
        ((ManualResult)report.Findings.ShouldHaveSingleItem().Result).Reason.ShouldContain(DimensionPattern.UnmappedCode);
    }

    private static async Task<(FixReport Report, string After)> RewriteAsync(string before)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("epplus");
        fix.Patterns.Returns([new DimensionPattern()]);
        fix.RemovedNamespaces.Returns(["OfficeOpenXml"]);
        fix.AddedNamespaces.Returns([]);
        Solution solution = TestSolution.Create(before.ReplaceLineEndings("\n"), EPPlusReferences.EPPlus);
        FixReport report = await new Fixer([fix]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        return (report, Translate(text));
    }

    // The scaffold of rows 2-5, translated by hand.
    private static string Translate(string text) => CellIndexer().Replace(text, ".Cell(\"$1\")")
        .Replace("using OfficeOpenXml;\n", "using ClosedXML.Excel;\n", StringComparison.Ordinal)
        .Replace("new ExcelPackage()", "new XLWorkbook()", StringComparison.Ordinal)
        .Replace("ExcelWorksheet ", "IXLWorksheet ", StringComparison.Ordinal)
        .Replace("package.Workbook.Worksheets", "package.Worksheets", StringComparison.Ordinal)
        .Replace("package.SaveAs(new FileInfo(path));", "package.SaveAs(path);", StringComparison.Ordinal);

    [GeneratedRegex("""\.Cells\["([A-Z]+[0-9]+)"\]""")]
    private static partial Regex CellIndexer();
}
