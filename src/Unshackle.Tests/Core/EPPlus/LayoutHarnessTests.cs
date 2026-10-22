using System.Globalization;
using ClosedXML.Excel;
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

// Council round 8 E1/E8 and round 12 E13/E14 for queue row 9 (epplus.layout). The Fixed shapes have full proofs in
// Proofs/EPPlus/Layout/_Combined (StylesCombinedProofTests, EPPlusHarnessProofTests); this test shows the harness sees a wrong layout,
// pins the E8 width difference, and checks the Manual shapes. The scaffold translation below is only used for the E8 and snapshot checks.
public class LayoutHarnessTests
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
                sheet.Cells["A1"].Value = "x";
                sheet.Cells["B3"].Value = 2;
        LAYOUT
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    private static readonly string s_directory = Path.Combine(AppContext.BaseDirectory, "Proofs", "EPPlus", "Layout", "_Combined");

    // Proof (Proofs/EPPlus/Layout/_Combined, the Fixer's real output, E14), the rewritten text, and a change that makes the layout wrong.
    public static TheoryData<string, string, string> Shapes() => new()
    {
        { "merge", "Range(\"A1:B2\")", "Range(\"A1:C2\")" },
        { "freeze-top-row", "Freeze(1, 0)", "Freeze(2, 0)" },
        { "freeze-row-column", "Freeze(2, 1)", "Freeze(2, 2)" },
        { "row-height", "Height = 30", "Height = 31" },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task LayoutProof_WrongLayout_IsSeenByHarness(string proof, string right, string wrong)
    {
        // Arrange
        string before = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".before.cs"), TestContext.Current.CancellationToken);
        string after = await File.ReadAllTextAsync(Path.Combine(s_directory, proof + ".after.cs"), TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after.Replace(right, wrong, StringComparison.Ordinal));

        // Assert
        after.ShouldContain(right);
        differences.ShouldNotBeEmpty("a wrong layout must show in the harness");
    }

    [Theory]
    [InlineData("sheet.Column(1).Width = 20;", LayoutPattern.WidthCode)]
    [InlineData("sheet.Cells[\"A1:B3\"].AutoFitColumns();", LayoutPattern.AutoFitCode)]
    [InlineData("sheet.Column(2).AutoFit();", LayoutPattern.AutoFitCode)]
    [InlineData("sheet.Cells[\"A1:B2\"].Merge = false;", LayoutPattern.UnmappedCode)]
    public async Task UnprovenShape_IsManualAndUnchanged(string layout, string code)
    {
        // Arrange
        string before = Before.Replace("LAYOUT", "        " + layout, StringComparison.Ordinal).ReplaceLineEndings("\n");

        // Act
        (FixReport report, string after) = await RewriteAsync(before, translate: false);

        // Assert
        ((ManualResult)report.Findings.ShouldHaveSingleItem().Result).Reason.ShouldContain(code);
        after.ShouldBe(before);
    }

    // WorkbookSnapshot itself now has freeze and row-height facts: CellCompare alone sees a wrong layout.
    [Theory]
    [InlineData("sheet.View.FreezePanes(2, 1);", "package.Worksheets.Add(\"S\");", "package.Worksheets.Add(\"S\"); sheet.SheetView.Freeze(2, 0);", "S freeze")]
    [InlineData("sheet.Row(2).Height = 30;", "package.Worksheets.Add(\"S\");", "package.Worksheets.Add(\"S\"); sheet.Row(2).Height = 31;", "S row 2 height")]
    public void Snapshot_WrongLayout_IsReported(string layout, string anchor, string wrong, string key)
    {
        // Arrange
        string before = Before.Replace("LAYOUT", "        " + layout, StringComparison.Ordinal);
        string after = Translate(Before.Replace("LAYOUT", string.Empty, StringComparison.Ordinal).ReplaceLineEndings("\n")).Replace(anchor, wrong, StringComparison.Ordinal);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldContain(d => d.StartsWith(key, StringComparison.Ordinal), string.Join(Environment.NewLine, differences));
    }

    // E8 evidence: the same width number is not the same column in the two libraries, so Column(i).Width stays Manual.
    [Fact]
    public void ColumnWidth_SameNumber_DiffersInHarness()
    {
        // Arrange
        string before = Before.Replace("LAYOUT", "        sheet.Column(1).Width = 20;", StringComparison.Ordinal);
        string after = Translate(before.ReplaceLineEndings("\n"));

        // Act
        List<string> differences = Differences(before, after);

        // Assert
        differences.ShouldContain(d => d.Contains("column 1 width", StringComparison.Ordinal));
    }

    private static List<string> Differences(string before, string after)
    {
        using var directory = new TempDirectory();
        string beforePath = Path.Combine(directory.Path, "before.xlsx");
        string afterPath = Path.Combine(directory.Path, "after.xlsx");
        WorkbookProgram.Run(before, EPPlusReferences.EPPlus, beforePath);
        WorkbookProgram.Run(after, EPPlusReferences.ClosedXml, afterPath);
        List<string> differences = [.. CellCompare.Differences(WorkbookSnapshot.Read(beforePath), WorkbookSnapshot.Read(afterPath))];
        string beforeLayout = Layout(beforePath);
        string afterLayout = Layout(afterPath);
        if (beforeLayout != afterLayout)
        {
            differences.Add($"layout: before '{beforeLayout}', after '{afterLayout}'");
        }

        return differences;
    }

    // Freeze panes and the heights of rows 1-5, read back with ClosedXML.
    private static string Layout(string path)
    {
        using var workbook = new XLWorkbook(path);
        IXLWorksheet sheet = workbook.Worksheet(1);
        IEnumerable<string> heights = Enumerable.Range(1, 5).Select(r => sheet.Row(r).Height.ToString("0.###", CultureInfo.InvariantCulture));
        return $"freeze={sheet.SheetView.SplitRow},{sheet.SheetView.SplitColumn} heights={string.Join(",", heights)}";
    }

    private static async Task<(FixReport Report, string After)> RewriteAsync(string before, bool translate = true)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("epplus");
        fix.Patterns.Returns([new LayoutPattern()]);
        fix.RemovedNamespaces.Returns(["OfficeOpenXml"]);
        fix.AddedNamespaces.Returns([]);
        Solution solution = TestSolution.Create(before.ReplaceLineEndings("\n"), EPPlusReferences.EPPlus);
        FixReport report = await new Fixer([fix]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        return (report, translate ? Translate(text) : text);
    }

    // The scaffold of rows 2-4, translated by hand.
    private static string Translate(string text) => text
        .Replace("using OfficeOpenXml;\n", "using ClosedXML.Excel;\n", StringComparison.Ordinal)
        .Replace("new ExcelPackage()", "new XLWorkbook()", StringComparison.Ordinal)
        .Replace("ExcelWorksheet sheet = package.Workbook.Worksheets.Add(\"S\");", "IXLWorksheet sheet = package.Worksheets.Add(\"S\");", StringComparison.Ordinal)
        .Replace("sheet.Cells[\"A1:B2\"]", "sheet.Range(\"A1:B2\")", StringComparison.Ordinal)
        .Replace("sheet.Cells[\"A1\"]", "sheet.Cell(\"A1\")", StringComparison.Ordinal)
        .Replace("sheet.Cells[\"B3\"]", "sheet.Cell(\"B3\")", StringComparison.Ordinal)
        .Replace("package.SaveAs(new FileInfo(path));", "package.SaveAs(path);", StringComparison.Ordinal);
}
