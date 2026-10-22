using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 9): sheet layout statements. Fixed, each proven by the harness: <c>range.Merge = true;</c> →
/// <c>range.Merge();</c>, <c>sheet.View.FreezePanes(r, c);</c> with literals → <c>sheet.SheetView.Freeze(r - 1, c - 1);</c> and
/// <c>sheet.Row(i).Height = h;</c> (same text, same unit). Column widths and auto-fit are Manual <c>ColumnWidthDiffers</c> (council
/// round 8, E8: the two libraries store a different width for the same number, and auto-fit measures fonts differently).
/// The receiver (sheet, range) is left to the other rows.
/// </summary>
public class LayoutPattern : IPattern
{
    // Council round 12, E13: auto-fit is Manual for good.
    public string Id => "epplus.layout";

    public string Description => "Merge = true / View.FreezePanes / Row(i).Height → ClosedXML; column width and auto-fit Manual (E8)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ExpressionStatementSyntax>().Where(s => LayoutMapping.Shape(s, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = (ExpressionStatementSyntax)node;
        (LayoutShape shape, ExpressionSyntax receiver) = LayoutMapping.Shape(statement, model)!.Value;
        string target = receiver.WithoutTrivia().ToString();
        return (shape, statement.Expression) switch
        {
            (LayoutShape.Merge, AssignmentExpressionSyntax { Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.TrueLiteralExpression } }) =>
                PatternResult.Fixed(SyntaxFactory.ParseStatement($"{target}.Merge();")),
            (LayoutShape.Merge, _) => Manual(EPPlusReason.UnmappedEPPlusLayout, "Merge", "only 'Merge = true' is mapped (unmerging or a computed value is not)"),
            (LayoutShape.Height, AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment) =>
                PatternResult.Fixed(SyntaxFactory.ParseStatement($"{target}.Height = {assignment.Right.WithoutTrivia()};")),
            (LayoutShape.Height, _) => Manual(EPPlusReason.UnmappedEPPlusLayout, "Height", "only a plain assignment of the row height is mapped"),
            (LayoutShape.FreezePanes, InvocationExpressionSyntax { ArgumentList.Arguments: [var row, var column] })
                when Literal(row.Expression) is int r && Literal(column.Expression) is int c && r >= 1 && c >= 1 =>
                PatternResult.Fixed(SyntaxFactory.ParseStatement($"{target}.SheetView.Freeze({r - 1}, {c - 1});")),
            (LayoutShape.FreezePanes, _) => Manual(EPPlusReason.UnmappedEPPlusLayout, "View.FreezePanes", "only literal row and column numbers (1 or more) are mapped"),
            (LayoutShape.Width, _) => Manual(EPPlusReason.ColumnWidthDiffers, "Column(i).Width",
                "EPPlus and ClosedXML store a different column width for the same number; set the width in ClosedXML by hand and check it (council round 8, E8)"),
            (LayoutShape.AutoFit, _) => Manual(EPPlusReason.AutoFitDiffers, "AutoFit",
                "the width auto-fit computes depends on the fonts installed on the machine and on each library's text measuring, so the result "
                + "cannot be proven equal; use AdjustToContents() and check the widths by hand (council round 12, E13)"),
            _ => Manual(EPPlusReason.UnmappedEPPlusLayout, shape.ToString(), "this layout statement is not mapped yet"),
        };
    }

    private static int? Literal(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression, Token.Value: int value } ? value : null;

    private static PatternResult Manual(EPPlusReason code, string shape, string why) =>
        PatternResult.Manual(code, $"EPPlus layout '{shape}': {why} (queue row 9). Migrate it by hand.");
}
