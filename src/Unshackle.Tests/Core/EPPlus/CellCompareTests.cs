using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// The harness itself (council round 8, E1): the same workbook written with EPPlus 4.5.3.3 and with ClosedXML compares equal, and each
// deliberate difference in one aspect (value, type, text, number format, font, fill, borders, merges, widths) makes it fail.
public class CellCompareTests
{
    private const string Before = """
        using System;
        using System.Drawing;
        using System.IO;
        using OfficeOpenXml;
        using OfficeOpenXml.Style;

        public static class Report
        {
            public static void Write(string path)
            {
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet sheet = package.Workbook.Worksheets.Add("Orders");
                    sheet.Cells[1, 1].Value = "Name";
                    sheet.Cells[1, 2].Value = "Amount";
                    sheet.Cells[2, 1].Value = "Widget";
                    sheet.Cells[2, 2].Value = 12.5;
                    sheet.Cells[2, 2].Style.Numberformat.Format = "0.00";
                    sheet.Cells[3, 1].Value = true;
                    sheet.Cells[3, 2].Value = new DateTime(2026, 10, 22);
                    sheet.Cells[3, 2].Style.Numberformat.Format = "yyyy-mm-dd";
                    sheet.Cells[1, 1].Style.Font.Bold = true;
                    sheet.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 0, 0));
                    sheet.Cells[2, 1].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    sheet.Cells["A5:B5"].Merge = true;
                    sheet.Cells[5, 1].Value = "Total";
                    package.SaveAs(new FileInfo(path));
                }
            }
        }
        """;

    private const string After = """
        using System;
        using ClosedXML.Excel;

        public static class Report
        {
            public static void Write(string path)
            {
                using (var workbook = new XLWorkbook())
                {
                    IXLWorksheet sheet = workbook.Worksheets.Add("Orders");
                    sheet.Cell(1, 1).Value = "Name";
                    sheet.Cell(1, 2).Value = "Amount";
                    sheet.Cell(2, 1).Value = "Widget";
                    sheet.Cell(2, 2).Value = 12.5;
                    sheet.Cell(2, 2).Style.NumberFormat.Format = "0.00";
                    sheet.Cell(3, 1).Value = true;
                    sheet.Cell(3, 2).Value = new DateTime(2026, 10, 22);
                    sheet.Cell(3, 2).Style.NumberFormat.Format = "yyyy-mm-dd";
                    sheet.Cell(1, 1).Style.Font.Bold = true;
                    sheet.Cell(1, 2).Style.Fill.PatternType = XLFillPatternValues.Solid;
                    sheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 0, 0);
                    sheet.Cell(2, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    sheet.Range("A5:B5").Merge();
                    sheet.Cell(5, 1).Value = "Total";
                    workbook.SaveAs(path);
                }
            }
        }
        """;

    [Fact]
    public void Differences_SameWorkbook_IsEmpty()
    {
        // Arrange, Act
        IReadOnlyList<string> differences = CellCompare.Differences(Before, After);

        // Assert
        differences.ShouldBeEmpty(string.Join(Environment.NewLine, differences));
    }

    public static TheoryData<string, string, string, string> DeliberateDifferences() => new()
    {
        { "value", "sheet.Cell(2, 1).Value = \"Widget\";", "sheet.Cell(2, 1).Value = \"Gadget\";", "Orders!A2 value" },
        { "type", "sheet.Cell(2, 2).Value = 12.5;", "sheet.Cell(2, 2).Value = \"12.5\";", "Orders!B2 value" },
        { "text", "sheet.Cell(2, 2).Style.NumberFormat.Format = \"0.00\";", "sheet.Cell(2, 2).Style.NumberFormat.Format = \"0.0\";", "Orders!B2 text" },
        { "number format", "sheet.Cell(3, 2).Style.NumberFormat.Format = \"yyyy-mm-dd\";", "sheet.Cell(3, 2).Style.NumberFormat.Format = \"yyyy-mm-dd;@\";", "Orders!B3 number format" },
        { "font", "sheet.Cell(1, 1).Style.Font.Bold = true;", "sheet.Cell(1, 1).Style.Font.Italic = true;", "Orders!A1 font" },
        { "fill", "XLColor.FromArgb(255, 255, 0, 0)", "XLColor.FromArgb(255, 0, 0, 255)", "Orders!B1 fill" },
        { "borders", "XLBorderStyleValues.Thin", "XLBorderStyleValues.Thick", "Orders!A2 borders" },
        { "merges", "sheet.Range(\"A5:B5\").Merge();", "sheet.Range(\"A5:C5\").Merge();", "Orders merges" },
        { "widths", "workbook.SaveAs(path);", "sheet.Column(1).Width = 21; workbook.SaveAs(path);", "Orders column 1 width" },
        { "sheet name", "Worksheets.Add(\"Orders\")", "Worksheets.Add(\"Order\")", "workbook sheets" },
    };

    [Theory]
    [MemberData(nameof(DeliberateDifferences))]
    public void Differences_DeliberateDifference_IsReported(string aspect, string line, string changed, string expectedKey)
    {
        // Arrange
        After.ShouldContain(line, Case.Sensitive, $"the {aspect} case must change a line of the equal snippet");
        string after = After.Replace(line, changed, StringComparison.Ordinal);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(Before, after);

        // Assert
        differences.ShouldContain(d => d.StartsWith(expectedKey + ":", StringComparison.Ordinal), string.Join(Environment.NewLine, differences));
    }

    // A mapping fact the harness found: EPPlus 4.5.3.3 stores Width as given, ClosedXML adds the cell padding, so the same number is a
    // different column in Excel. A width rewrite is not a rename (claude/tasks/epplus-queue.md).
    [Fact]
    public void Differences_SameColumnWidthNumber_IsReported()
    {
        // Arrange
        string before = Before.Replace("package.SaveAs(", "sheet.Column(1).Width = 20; package.SaveAs(", StringComparison.Ordinal);
        string after = After.Replace("workbook.SaveAs(", "sheet.Column(1).Width = 20; workbook.SaveAs(", StringComparison.Ordinal);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldContain(d => d.StartsWith("Orders column 1 width:", StringComparison.Ordinal), string.Join(Environment.NewLine, differences));
    }
}
