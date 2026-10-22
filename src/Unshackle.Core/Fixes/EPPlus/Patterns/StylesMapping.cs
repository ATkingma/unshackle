using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// The EPPlus → ClosedXML style shapes of StylesPattern (queue row 8). Each Fixed shape has a cell-compare harness proof (council round 8,
// E1) in src/Unshackle.Tests/Core/EPPlus/StylesHarnessTests.cs.
internal static class StylesMapping
{
    // EPPlus path after ExcelStyle → ClosedXML path; the value is copied as it is.
    private static readonly Dictionary<string, string> s_plain = new(StringComparer.Ordinal)
    {
        ["Font.Bold"] = "Font.Bold",
        ["Font.Italic"] = "Font.Italic",
        ["Font.Strike"] = "Font.Strikethrough",
        ["Font.Size"] = "Font.FontSize",
        ["Font.Name"] = "Font.FontName",
        ["Numberformat.Format"] = "NumberFormat.Format",
        ["WrapText"] = "Alignment.WrapText",
    };

    // EPPlus path → (ClosedXML path, EPPlus enum, ClosedXML enum); the enum member names are the same on both sides.
    private static readonly Dictionary<string, (string Target, string From, string To)> s_enums = new(StringComparer.Ordinal)
    {
        ["HorizontalAlignment"] = ("Alignment.Horizontal", "ExcelHorizontalAlignment", "XLAlignmentHorizontalValues"),
        ["VerticalAlignment"] = ("Alignment.Vertical", "ExcelVerticalAlignment", "XLAlignmentVerticalValues"),
        ["Border.Top.Style"] = ("Border.TopBorder", "ExcelBorderStyle", "XLBorderStyleValues"),
        ["Border.Bottom.Style"] = ("Border.BottomBorder", "ExcelBorderStyle", "XLBorderStyleValues"),
        ["Border.Left.Style"] = ("Border.LeftBorder", "ExcelBorderStyle", "XLBorderStyleValues"),
        ["Border.Right.Style"] = ("Border.RightBorder", "ExcelBorderStyle", "XLBorderStyleValues"),
    };

    // Members that exist with the same meaning in the ClosedXML enum.
    private static readonly Dictionary<string, HashSet<string>> s_members = new(StringComparer.Ordinal)
    {
        ["ExcelHorizontalAlignment"] = new(["General", "Left", "Center", "CenterContinuous", "Right", "Fill", "Distributed", "Justify"], StringComparer.Ordinal),
        ["ExcelVerticalAlignment"] = new(["Top", "Center", "Bottom", "Distributed", "Justify"], StringComparer.Ordinal),
        ["ExcelBorderStyle"] = new(
            ["None", "Hair", "Dotted", "DashDot", "Thin", "MediumDashDotDot", "DashDotDot", "MediumDashed", "Dashed", "MediumDashDot", "Thick", "Medium", "Double"],
            StringComparer.Ordinal),
    };

    // The ExcelStyle expression a member chain starts from, and the names after it (x.Style.Font.Bold → (x.Style, [Font, Bold])).
    public static (ExpressionSyntax Style, IReadOnlyList<string> Path)? Split(ExpressionSyntax? expression, SemanticModel model)
    {
        List<string> names = [];
        while (expression is MemberAccessExpressionSyntax access)
        {
            names.Insert(0, access.Name.Identifier.ValueText);
            if (IsStyle(model.GetTypeInfo(access.Expression).Type))
            {
                return (access.Expression, names);
            }

            expression = access.Expression;
        }

        return null;
    }

    // The ExcelStyle a statement starts from (the left side of an assignment, or the method of a call), or null: the statements of row 8.
    public static ExpressionSyntax? StyleOf(ExpressionStatementSyntax statement, SemanticModel model) =>
        Split(statement.Expression switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left,
            InvocationExpressionSyntax invocation => invocation.Expression,
            _ => null,
        }, model)?.Style;

    // The ClosedXML path and value of an assignment, or null when the shape or value is not mapped.
    public static (string Target, string Value)? Assignment(string shape, ExpressionSyntax value, SemanticModel model)
    {
        if (s_plain.TryGetValue(shape, out string? target))
        {
            return (target, value.WithoutTrivia().ToString());
        }

        return s_enums.TryGetValue(shape, out (string Target, string From, string To) mapped) && EnumMember(value, model, mapped.From) is string member && s_members[mapped.From].Contains(member)
            ? (mapped.Target, $"{mapped.To}.{member}")
            : null;
    }

    // The member name of a literal EPPlus enum value (ExcelBorderStyle.Thin → Thin), or null.
    public static string? EnumMember(ExpressionSyntax value, SemanticModel model, string enumName) =>
        model.GetSymbolInfo(value).Symbol is IFieldSymbol { ContainingType: { TypeKind: TypeKind.Enum } type } field
        && type.Name == enumName && IsEPPlus(type)
            ? field.Name
            : null;

    private static bool IsStyle(ITypeSymbol? type) => type is INamedTypeSymbol { Name: "ExcelStyle" } named && IsEPPlus(named);

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
