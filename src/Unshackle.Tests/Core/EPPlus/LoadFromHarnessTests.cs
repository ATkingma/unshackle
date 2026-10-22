using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Council round 8, E1/E4 for queue row 10 (epplus.load-from): LoadFromCollection without headers → InsertData, one harness case per
// covered property type (and nulls). The scaffold (rows 2-5) is translated by hand. Each case also fails when the data lands wrong.
public partial class LoadFromHarnessTests
{
    private const string Before = """
        using System.Collections.Generic;
        using System.IO;
        using OfficeOpenXml;

        public class Item
        {
        PROPERTIES
        }

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new ExcelPackage();
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("S");
                var items = new List<Item> { ITEMS };
                sheet.Cells["B2"].LoadFromCollection(items);
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    public static TheoryData<string, string> Shapes() => new()
    {
        { "public string Name { get; set; }", "new Item { Name = \"a\" }, new Item { Name = null }, new Item { Name = \"\" }" },
        { "public int Count { get; set; }", "new Item { Count = 1 }, new Item { Count = -7 }" },
        { "public long Big { get; set; }", "new Item { Big = 1234567890123 }" },
        { "public double Ratio { get; set; }", "new Item { Ratio = 0.1 }, new Item { Ratio = 1e10 }" },
        { "public bool Flag { get; set; }", "new Item { Flag = true }, new Item { Flag = false }" },
        { "public decimal Price { get; set; }", "new Item { Price = 12.34m }" },
        { "public int? Maybe { get; set; }", "new Item { Maybe = 3 }, new Item { Maybe = null }" },
        { "public System.DayOfWeek Day { get; set; }", "new Item { Day = System.DayOfWeek.Friday }, new Item()" },
        { "public System.DayOfWeek? Day { get; set; }", "new Item { Day = System.DayOfWeek.Monday }, new Item { Day = null }" },
        {
            "public string Name { get; set; } public int Count { get; set; } public double Ratio { get; set; } public bool Flag { get; set; }",
            "new Item { Name = \"x\", Count = 2, Ratio = 2.5, Flag = true }, new Item { Name = \"y\" }"
        },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task LoadFromCollection_RewrittenByPattern_WritesSameWorkbook(string properties, string items)
    {
        // Arrange
        string before = Before.Replace("PROPERTIES", "    " + properties, StringComparison.Ordinal).Replace("ITEMS", items, StringComparison.Ordinal);

        // Act
        (FixReport report, string after) = await RewriteAsync(before);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<FixedResult>();
        after.ShouldContain("sheet.Cell(\"B2\").InsertData(items);");
        CellCompare.Differences(before, after).ShouldBeEmpty();
        CellCompare.Differences(before, after.Replace("Cell(\"B2\").InsertData", "Cell(\"B3\").InsertData", StringComparison.Ordinal))
            .ShouldNotBeEmpty("data in the wrong place must show in the harness");
    }

    [Theory]
    [InlineData("public System.DateTime When { get; set; }", "sheet.Cells[\"B2\"].LoadFromCollection(items);", nameof(EPPlusReason.LoadFromTypeDiffers))]
    [InlineData("public System.DateTime? When { get; set; }", "sheet.Cells[\"B2\"].LoadFromCollection(items);", nameof(EPPlusReason.LoadFromTypeDiffers))]
    [InlineData("public string Name { get; set; }", "sheet.Cells[\"B2\"].LoadFromCollection(items, true);", nameof(EPPlusReason.LoadFromHeadersNotMapped))]
    [InlineData("public string Name;", "sheet.Cells[\"B2\"].LoadFromCollection(items);", nameof(EPPlusReason.UncoveredRuntimeType))]
    [InlineData("public string Name { get; set; }", "var range = sheet.Cells[\"B2\"].LoadFromCollection(items);", nameof(EPPlusReason.UnmappedLoadFrom))]
    public async Task UnprovenShape_IsManual(string properties, string call, string code)
    {
        // Arrange
        string before = Before.Replace("PROPERTIES", "    " + properties, StringComparison.Ordinal).Replace("ITEMS", string.Empty, StringComparison.Ordinal)
            .Replace("sheet.Cells[\"B2\"].LoadFromCollection(items);", call, StringComparison.Ordinal);

        // Act
        (FixReport report, _) = await RewriteAsync(before);

        // Assert
        ((ManualResult)report.Findings.ShouldHaveSingleItem().Result).Reason.ShouldContain(code);
    }

    // Council round 12, E15: a DateTime property written by hand as InsertData differs in the harness, so the pattern keeps it Manual
    // (LoadFromTypeDiffers). Enum properties compare equal and are in Shapes.
    [Theory]
    [InlineData("public System.DateTime When { get; set; }", "new Item { When = new System.DateTime(2024, 3, 1, 13, 30, 0) }")]
    public void DateTime_HarnessReportsDifference(string properties, string items)
    {
        // Arrange
        string before = Before.Replace("PROPERTIES", "    " + properties, StringComparison.Ordinal).Replace("ITEMS", items, StringComparison.Ordinal);
        string after = Translate(before.ReplaceLineEndings("\n").Replace(".LoadFromCollection(items)", ".InsertData(items)", StringComparison.Ordinal));

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldNotBeEmpty();
    }

    private static async Task<(FixReport Report, string After)> RewriteAsync(string before)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("epplus");
        fix.Patterns.Returns([new LoadFromPattern()]);
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
