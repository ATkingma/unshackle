using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Queue row 8 with the whole fix. Until rows 3-4 migrate the receiver (ExcelRange), epplus.unsupported still names the receiver's
// .Style; the rewritten statements carry StylesPattern.MigratedAnnotationKind so it can skip them (claim request, see TAAK-309 log).
public class StylesUnsupportedTests
{
    private const string Code = """
        using OfficeOpenXml;

        internal static class Report
        {
            internal static void Format(ExcelRange cell)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Numberformat.Format = "0.00";
            }
        }
        """;

    [Fact]
    public async Task Fix_StyleStatementWithEPPlusReceiver_StaysManualAllOrNothing()
    {
        // Arrange
        Solution solution = TestSolution.Create(Code, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        report.Solution.ShouldBeSameAs(solution);
        // TAAK-337 (E25, with E24): the ExcelRange parameter now has its epplus.flow-types row instead of an epplus.unsupported one.
        report.Findings.ShouldContain(f => f.PatternId == "epplus.flow-types" && ((ManualResult)f.Result).Reason.Contains("takes an ExcelRange", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Fix_MigratedStyleStatement_UnsupportedNamesOnlyTheReceiver()
    {
        // Arrange
        Solution solution = TestSolution.Create(Code, EPPlusReferences.EPPlus);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        string reasons = string.Join(" | ", report.Findings.Where(f => f.PatternId == "epplus.unsupported").Select(f => ((ManualResult)f.Result).Reason));
        reasons.ShouldNotContain("row 8", Case.Sensitive, reasons);
        report.Findings.ShouldContain(f => f.PatternId == "epplus.flow-types", reasons);
    }

    [Fact]
    public async Task Rewrite_FixedStatement_CarriesMigratedAnnotation()
    {
        // Arrange
        SemanticModel model = (await TestSolution.Create(Code, EPPlusReferences.EPPlus).Projects.Single().Documents.Single()
            .GetSemanticModelAsync(TestContext.Current.CancellationToken))!;
        SyntaxNode root = await model.SyntaxTree.GetRootAsync(TestContext.Current.CancellationToken);
        var pattern = new StylesPattern();

        // Act
        PatternResult[] results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.Length.ShouldBe(2);
        results.ShouldAllBe(r => r is FixedResult && ((FixedResult)r).NewNodes.Single().HasAnnotations(StylesPattern.MigratedAnnotationKind));
        ((FixedResult)results[1]).NewNodes.Single().ShouldBeAssignableTo<ExpressionStatementSyntax>()!.ToString().ShouldBe("cell.Style.NumberFormat.Format = \"0.00\";");
    }
}
