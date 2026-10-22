using ClosedXML.Excel;
using Shouldly;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 12, E12: GetValue<T>() would be Fixed per T only when every cell kind (empty, bool, whole number, fraction, date, text, numeric
// text) gives the same result, or both throw the same exception type, in both contexts: a workbook opened from a file, and cells the same
// code wrote. The matrix shows no T qualifies; these tests pin exactly where each T differs, so a library upgrade that changes it is noticed.
public class GetValueMatrixTests
{
    // A1..A7 of the source workbook, written by ClosedXML (as Excel would store them), and how each library writes the same value itself.
    private static readonly (string Name, Action<IXLCell> Write, string EPPlusWrite, string ClosedXmlWrite)[] s_cells =
    [
        ("empty", _ => { }, "", ""),
        ("bool", c => c.Value = true, "= true", "= true"),
        ("whole number", c => c.Value = 42, "= 42", "= 42"),
        ("fraction", c => c.Value = 42.5, "= 42.5", "= 42.5"),
        ("date", c => { c.Value = new DateTime(2026, 10, 22); }, "= new DateTime(2026, 10, 22)", "= new DateTime(2026, 10, 22)"),
        ("text", c => c.Value = "abc", "= \"abc\"", "= \"abc\""),
        ("numeric text", c => c.Value = "42", "= \"42\"", "= \"42\""),
    ];

    // The cells (by index into s_cells) where EPPlus 4.5.3.3 and ClosedXML 0.105.1 differ, per T; the same in both contexts. Every T differs
    // on an empty cell at least (EPPlus returns default(T), ClosedXML throws), so no T is covered and GetValue<T> stays CellReadDiffers.
    public static TheoryData<string, string> Differing() => new()
    {
        { "int", "0 3 4 5" },
        { "long", "0 3 4 5" },
        { "double", "0 4 5" },
        { "decimal", "0 4 5" },
        { "DateTime", "0 2 3 5 6" },
        { "bool", "0 5 6" },
    };

    [Theory]
    [MemberData(nameof(Differing))]
    public void GetValue_OpenedWorkbook_DiffersOnTheseCells(string type, string expected)
    {
        // Arrange
        using var directory = new TempDirectory();
        var differing = new List<int>();

        // Act
        for (int row = 0; row < s_cells.Length; row++)
        {
            string source = Path.Combine(directory.Path, $"source{row}.xlsx");
            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet sheet = workbook.Worksheets.Add("Data");
                s_cells[row].Write(sheet.Cell(1, 1));
                sheet.Cell(2, 1).Value = "keep";
                workbook.SaveAs(source);
            }

            string read = $"Probe.Read(() => sheet.Cells[1, 1].GetValue<{type}>())";
            string before = Snippet("using OfficeOpenXml;", $"using var package = new ExcelPackage(new FileInfo(@\"{source}\")); var sheet = package.Workbook.Worksheets[\"Data\"];", read);
            string after = Snippet("using ClosedXML.Excel;", $"using var package = new XLWorkbook(@\"{source}\"); var sheet = package.Worksheet(\"Data\");",
                read.Replace("sheet.Cells[1, 1]", "sheet.Cell(1, 1)", StringComparison.Ordinal));
            (string epplus, string closedXml) = Results(before, after, directory.Path);
            if (epplus != closedXml)
            {
                differing.Add(row);
            }
        }

        // Assert
        string.Join(" ", differing).ShouldBe(expected, $"GetValue<{type}> in an opened workbook");
    }

    [Theory]
    [MemberData(nameof(Differing))]
    public void GetValue_WrittenCell_DiffersOnTheseCells(string type, string expected)
    {
        // Arrange
        using var directory = new TempDirectory();
        var differing = new List<int>();

        // Act
        for (int row = 0; row < s_cells.Length; row++)
        {
            (_, _, string epplusWrite, string closedXmlWrite) = s_cells[row];
            string read = $"Probe.Read(() => sheet.Cells[1, 1].GetValue<{type}>())";
            string before = Snippet("using OfficeOpenXml;",
                "using var package = new ExcelPackage(); var sheet = package.Workbook.Worksheets.Add(\"Data\");"
                + (epplusWrite.Length == 0 ? "" : $" sheet.Cells[1, 1].Value {epplusWrite};"), read);
            string after = Snippet("using ClosedXML.Excel;",
                "using var package = new XLWorkbook(); var sheet = package.Worksheets.Add(\"Data\");"
                + (closedXmlWrite.Length == 0 ? "" : $" sheet.Cell(1, 1).Value {closedXmlWrite};"),
                read.Replace("sheet.Cells[1, 1]", "sheet.Cell(1, 1)", StringComparison.Ordinal));
            (string epplus, string closedXml) = Results(before, after, directory.Path);
            if (epplus != closedXml)
            {
                differing.Add(row);
            }
        }

        // Assert
        string.Join(" ", differing).ShouldBe(expected, $"GetValue<{type}> of cells written by the same code");
    }

    // The snippet writes what it read (or the exception type) as text into a fresh result workbook, so the harness compares it.
    private static string Snippet(string usingLine, string open, string read) => $$"""
        using System;
        using System.Globalization;
        using System.IO;
        {{usingLine}}

        public static class Probe
        {
            public static string Read<T>(Func<T> read)
            {
                try
                {
                    return Convert.ToString(read(), CultureInfo.InvariantCulture) ?? "<null>";
                }
                catch (Exception ex)
                {
                    return "throws " + ex.GetType().Name;
                }
            }

            public static void Write(string path)
            {
                string result;
                {
                    {{open}}
                    result = {{read}};
                }

                File.WriteAllText(path, result);
            }
        }
        """;

    private static (string EPPlus, string ClosedXml) Results(string before, string after, string directory)
    {
        string beforePath = Path.Combine(directory, "before.txt");
        string afterPath = Path.Combine(directory, "after.txt");
        WorkbookProgram.Run(before, EPPlusReferences.EPPlus, beforePath);
        WorkbookProgram.Run(after, EPPlusReferences.ClosedXml, afterPath);
        return (File.ReadAllText(beforePath), File.ReadAllText(afterPath));
    }
}
