using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// The shapes of DimensionPattern (queue row 7).
internal static class DimensionMapping
{
    // (End|Start, Row|Column) → ClosedXML call chain after the sheet.
    private static readonly Dictionary<(string Corner, string Part), (string Used, string Number)> s_calls = new()
    {
        [("End", "Row")] = ("LastRowUsed(XLCellsUsedOptions.All)", "RowNumber()"),
        [("End", "Column")] = ("LastColumnUsed(XLCellsUsedOptions.All)", "ColumnNumber()"),
        [("Start", "Row")] = ("FirstRowUsed(XLCellsUsedOptions.All)", "RowNumber()"),
        [("Start", "Column")] = ("FirstColumnUsed(XLCellsUsedOptions.All)", "ColumnNumber()"),
    };

    // sheet.Dimension with sheet an EPPlus ExcelWorksheet.
    public static bool IsDimension(MemberAccessExpressionSyntax dimension, SemanticModel model) =>
        model.GetTypeInfo(dimension.Expression).Type is INamedTypeSymbol { Name: "ExcelWorksheet" } sheet && IsEPPlus(sheet);

    // The node to report: the whole mapped chain (sheet.Dimension.End.Row, sheet.Dimension?.End.Row), else the first member after
    // Dimension, else Dimension itself.
    public static SyntaxNode Outer(MemberAccessExpressionSyntax dimension) => dimension.Parent switch
    {
        MemberAccessExpressionSyntax corner when corner.Expression == dimension =>
            corner.Parent is MemberAccessExpressionSyntax part && part.Expression == corner ? part : corner,
        ConditionalAccessExpressionSyntax conditional when conditional.Expression == dimension => conditional,
        _ => dimension,
    };

    // The ClosedXML text of a mapped chain, or null.
    public static string? Rewrite(SyntaxNode node) => node switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: var part, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var corner, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Dimension" } dimension } }
            when s_calls.TryGetValue((corner, part), out (string Used, string Number) call) =>
            $"{dimension.Expression.WithoutTrivia()}.{call.Used}.{call.Number}",
        ConditionalAccessExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Dimension" } dimension,
            WhenNotNull: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var part, Expression: MemberBindingExpressionSyntax { Name.Identifier.ValueText: var corner } },
        } when s_calls.TryGetValue((corner, part), out (string Used, string Number) call) =>
            $"{dimension.Expression.WithoutTrivia()}.{call.Used}?.{call.Number}",
        _ => null,
    };

    // Names rows 7 and 10 write onto an EPPlus receiver (sheet.LastRowUsed(), range.InsertData(..)); they bind to nothing until rows 3-4
    // migrate the receiver. epplus.unsupported skips them and reports the receiver only.
    private static readonly HashSet<string> s_rewrittenNames =
        new(["LastRowUsed", "LastColumnUsed", "FirstRowUsed", "FirstColumnUsed", "RowNumber", "ColumnNumber", "InsertData"], StringComparer.Ordinal);

    public static bool IsRewrittenName(SyntaxNode name, SemanticModel model) =>
        name is SimpleNameSyntax simple && s_rewrittenNames.Contains(simple.Identifier.ValueText) && model.GetSymbolInfo(name).Symbol is null;

    private static bool IsEPPlus(INamedTypeSymbol type)
    {
        INamespaceSymbol? ns = type.ContainingNamespace;
        while (ns is { ContainingNamespace.IsGlobalNamespace: false })
        {
            ns = ns.ContainingNamespace;
        }

        return ns?.Name == EPPlusSyntax.Namespace;
    }
}
