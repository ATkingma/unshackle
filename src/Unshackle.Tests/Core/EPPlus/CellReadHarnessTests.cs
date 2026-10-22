using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Queue rows 4-5: reads and ranges. Reads are Manual (CellReadDiffers) because of the rows in Read_Differs; the rest is proven by writing the read result back into a cell (B1), so the harness compares what each library read.
public class CellReadHarnessTests
{
    private const string Before = """
        using System;
        using System.IO;
        using OfficeOpenXml;

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new ExcelPackage();
                var sheet = package.Workbook.Worksheets.Add("Data");
                SETUP
                sheet.Cells[1, 2].Value = READ;
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    private const string After = """
        using System;
        using System.IO;
        using ClosedXML.Excel;

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new XLWorkbook();
                var sheet = package.Worksheets.Add("Data");
                SETUP
                sheet.Cell(1, 2).Value = READ;
                package.SaveAs(path);
            }
        }
        """;

    public static TheoryData<string, string, string, string, string> DifferingReads() => new()
    {
        { "text of bool", "sheet.Cells[1, 1].Value = true;", "sheet.Cells[1, 1].Text", "sheet.Cell(1, 1).Value = true;", "sheet.Cell(1, 1).GetFormattedString()" },
        { "GetValue<string> of empty", "", "sheet.Cells[1, 1].GetValue<string>() ?? \"<null>\"", "", "sheet.Cell(1, 1).GetValue<string>() ?? \"<null>\"" },
    };

    [Theory]
    [MemberData(nameof(DifferingReads))]
    public void Read_Differs(string name, string beforeSetup, string beforeRead, string afterSetup, string afterRead)
    {
        // Arrange
        string before = Before.Replace("SETUP", beforeSetup, StringComparison.Ordinal).Replace("READ", beforeRead, StringComparison.Ordinal);
        string after = After.Replace("SETUP", afterSetup, StringComparison.Ordinal).Replace("READ", afterRead, StringComparison.Ordinal);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldNotBeEmpty(name);
    }
}
