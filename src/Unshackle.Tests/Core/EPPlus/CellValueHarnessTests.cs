using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 8, E4 (queue row 5): `cell.Value = x` is Fixed per static type only when the harness proves EPPlus 4.5.3.3 and ClosedXML
// write the same cell for it. Each row is a type with a few edge values; a type without a row here is Manual (UncoveredRuntimeType).
public class CellValueHarnessTests
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
                DECLARATION
                sheet.Cells[1, 1].Value = value;
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
                DECLARATION
                sheet.Cell(1, 1).Value = value;
                package.SaveAs(path);
            }
        }
        """;

    // Harness facts behind E4: ClosedXML writes a number with 15 significant digits, EPPlus 4.5.3.3 with 17 (round-trip), and EPPlus writes a
    // DateTime as a General-format serial number where ClosedXML writes a date format. So these stay Manual (UncoveredRuntimeType), and
    // a double, float, decimal or long is Fixed only as a constant whose value round-trips in 15 digits.
    [Theory]
    [InlineData("float value = 0.1f;")]
    [InlineData("double value = 0.1 + 0.2;")]
    [InlineData("decimal value = 79228162514264337593543950335m;")]
    [InlineData("DateTime value = new DateTime(2026, 10, 22);")]
    public void Value_UncoveredType_Differs(string declaration)
    {
        // Arrange, Act
        IReadOnlyList<string> differences = CellCompare.Differences(
            Before.Replace("DECLARATION", declaration, StringComparison.Ordinal), After.Replace("DECLARATION", declaration, StringComparison.Ordinal));

        // Assert
        differences.ShouldContain(d => d.StartsWith("Data!A1 value:", StringComparison.Ordinal));
    }
}
