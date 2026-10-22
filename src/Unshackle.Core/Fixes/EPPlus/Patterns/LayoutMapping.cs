using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// Recognises the layout statements of LayoutPattern by the EPPlus type of their receiver (queue row 9).
internal static class LayoutMapping
{
    // The shape of a statement and the receiver that stays (the range, row, column or sheet), or null when it is not a layout statement.
    public static (LayoutShape Shape, ExpressionSyntax Receiver)? Shape(ExpressionStatementSyntax statement, SemanticModel model) => statement.Expression switch
    {
        AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } left } =>
            (name, TypeName(left.Expression, model)) switch
            {
                ("Merge", "ExcelRange" or "ExcelRangeBase") => (LayoutShape.Merge, left.Expression),
                ("Height", "ExcelRow") => (LayoutShape.Height, left.Expression),
                ("Width", "ExcelColumn") => (LayoutShape.Width, left.Expression),
                _ => null,
            },
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } call } =>
            (name, TypeName(call.Expression, model)) switch
            {
                ("FreezePanes", "ExcelWorksheetView") when call.Expression is MemberAccessExpressionSyntax view => (LayoutShape.FreezePanes, view.Expression),
                ("AutoFitColumns", "ExcelRange" or "ExcelRangeBase") => (LayoutShape.AutoFit, call.Expression),
                ("AutoFit", "ExcelColumn") => (LayoutShape.AutoFit, call.Expression),
                _ => null,
            },
        _ => null,
    };

    // The EPPlus type name of an expression, or null when it is not an EPPlus type.
    private static string? TypeName(ExpressionSyntax expression, SemanticModel model)
    {
        if (model.GetTypeInfo(expression).Type is not INamedTypeSymbol type)
        {
            return null;
        }

        INamespaceSymbol? ns = type.ContainingNamespace;
        while (ns is { ContainingNamespace.IsGlobalNamespace: false })
        {
            ns = ns.ContainingNamespace;
        }

        return ns?.Name == EPPlusSyntax.Namespace ? type.Name : null;
    }
}
