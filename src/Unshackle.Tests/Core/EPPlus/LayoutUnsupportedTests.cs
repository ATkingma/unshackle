using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Queue row 9 with the whole fix: epplus.unsupported names only the receiver of a layout statement, not the rewritten tail.
public class LayoutUnsupportedTests
{
    [Fact]
    public async Task Fix_RewrittenLayoutStatements_UnsupportedNamesOnlyTheReceiver()
    {
        // Arrange
        const string code = """
            using OfficeOpenXml;

            internal class Report
            {
                // A constructor, so the row is epplus.unsupported's (a helper method would be epplus.flow-types', E24).
                internal Report(ExcelWorksheet sheet)
                {
                    sheet.Cells["A1:B2"].Merge = true;
                    sheet.View.FreezePanes(2, 1);
                    sheet.Row(3).Height = 30;
                }
            }
            """;
        Solution solution = TestSolution.Create(code, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        string reasons = string.Join(" | ", report.Findings.Where(f => f.PatternId == "epplus.unsupported").Select(f => ((ManualResult)f.Result).Reason));
        reasons.ShouldNotContain("row 9", Case.Sensitive, reasons);
        reasons.ShouldContain("ExcelWorksheet", Case.Sensitive, reasons);
    }
}
