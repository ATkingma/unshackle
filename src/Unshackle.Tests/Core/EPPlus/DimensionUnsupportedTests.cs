using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Rows 7 and 10 with the whole fix: epplus.unsupported names only the receiver (sheet, range) of a rewritten Dimension or LoadFrom
// expression, not the ClosedXML members the rewrite put on it.
public class DimensionUnsupportedTests
{
    [Fact]
    public async Task Fix_RewrittenDimensionAndLoadFrom_UnsupportedNamesOnlyTheReceiver()
    {
        // Arrange
        const string code = """
            using System.Collections.Generic;
            using OfficeOpenXml;

            public class Item
            {
                public string Name { get; set; } = string.Empty;
            }

            internal static class Report
            {
                internal static int Fill(ExcelWorksheet sheet, List<Item> items)
                {
                    sheet.Cells["A1"].LoadFromCollection(items);
                    return sheet.Dimension.End.Row;
                }
            }
            """;
        Solution solution = TestSolution.Create(code, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        string reasons = string.Join(" | ", report.Findings.Where(f => f.PatternId == "epplus.unsupported").Select(f => ((ManualResult)f.Result).Reason));
        reasons.ShouldNotContain("LastRowUsed", Case.Sensitive, reasons);
        reasons.ShouldNotContain("InsertData", Case.Sensitive, reasons);
        reasons.ShouldContain("ExcelWorksheet", Case.Sensitive, reasons);
    }
}
