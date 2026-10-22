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

    public static TheoryData<string, string, string, string, string> Reads() => new()
    {
        // name, EPPlus setup, EPPlus read, ClosedXML setup, ClosedXML read
        { "text of string", "sheet.Cells[1, 1].Value = \"Widget\";", "sheet.Cells[1, 1].Text", "sheet.Cell(1, 1).Value = \"Widget\";", "sheet.Cell(1, 1).GetFormattedString()" },
        { "text of int", "sheet.Cells[1, 1].Value = 42;", "sheet.Cells[1, 1].Text", "sheet.Cell(1, 1).Value = 42;", "sheet.Cell(1, 1).GetFormattedString()" },
        { "text of double general", "sheet.Cells[1, 1].Value = 12.5;", "sheet.Cells[1, 1].Text", "sheet.Cell(1, 1).Value = 12.5;", "sheet.Cell(1, 1).GetFormattedString()" },
        { "text of double 0.00", "sheet.Cells[1, 1].Value = 12.5; sheet.Cells[1, 1].Style.Numberformat.Format = \"0.00\";", "sheet.Cells[1, 1].Text", "sheet.Cell(1, 1).Value = 12.5; sheet.Cell(1, 1).Style.NumberFormat.Format = \"0.00\";", "sheet.Cell(1, 1).GetFormattedString()" },
        { "text of empty", "", "sheet.Cells[1, 1].Text", "", "sheet.Cell(1, 1).GetFormattedString()" },
        { "GetValue<string> of int", "sheet.Cells[1, 1].Value = 42;", "sheet.Cells[1, 1].GetValue<string>()", "sheet.Cell(1, 1).Value = 42;", "sheet.Cell(1, 1).GetValue<string>()" },
        { "GetValue<int> of int", "sheet.Cells[1, 1].Value = 42;", "sheet.Cells[1, 1].GetValue<int>()", "sheet.Cell(1, 1).Value = 42;", "sheet.Cell(1, 1).GetValue<int>()" },
        { "GetValue<double> of double", "sheet.Cells[1, 1].Value = 12.5;", "sheet.Cells[1, 1].GetValue<double>()", "sheet.Cell(1, 1).Value = 12.5;", "sheet.Cell(1, 1).GetValue<double>()" },
        { "GetValue<string> of string", "sheet.Cells[1, 1].Value = \"Widget\";", "sheet.Cells[1, 1].GetValue<string>()", "sheet.Cell(1, 1).Value = \"Widget\";", "sheet.Cell(1, 1).GetValue<string>()" },
        { "GetValue<bool> of bool", "sheet.Cells[1, 1].Value = true;", "sheet.Cells[1, 1].GetValue<bool>()", "sheet.Cell(1, 1).Value = true;", "sheet.Cell(1, 1).GetValue<bool>()" },
        { "range value", "sheet.Cells[\"A1:C2\"].Value = \"x\";", "\"done\"", "sheet.Range(\"A1:C2\").Value = \"x\";", "\"done\"" },
        { "range value by numbers", "sheet.Cells[1, 1, 2, 3].Value = 7;", "\"done\"", "sheet.Range(1, 1, 2, 3).Value = 7;", "\"done\"" },
        { "range bold", "sheet.Cells[\"A1:C2\"].Style.Font.Bold = true;", "\"done\"", "sheet.Range(\"A1:C2\").Style.Font.Bold = true;", "\"done\"" },
    };

    [Theory]
    [MemberData(nameof(Reads))]
    public void Read_SameResult(string name, string beforeSetup, string beforeRead, string afterSetup, string afterRead)
    {
        // Arrange
        string before = Before.Replace("SETUP", beforeSetup, StringComparison.Ordinal).Replace("READ", beforeRead, StringComparison.Ordinal);
        string after = After.Replace("SETUP", afterSetup, StringComparison.Ordinal).Replace("READ", afterRead, StringComparison.Ordinal);

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldBeEmpty(name + Environment.NewLine + string.Join(Environment.NewLine, differences));
    }

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
