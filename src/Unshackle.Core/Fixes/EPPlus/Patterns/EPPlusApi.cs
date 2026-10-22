using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// Which EPPlus API a name binds to, and which row of claude/tasks/epplus-queue.md migrates it (for the Manual reasons of UnsupportedPattern).
internal static class EPPlusApi
{
    public static readonly HashSet<string> TestFrameworks =
        new(["Microsoft.TestPlatform.CoreUtilities", "Microsoft.VisualStudio.TestPlatform.TestFramework", "xunit.core", "xunit.v3.core", "nunit.framework"], StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> s_rows = new(StringComparer.Ordinal)
    {
        ["ExcelPackage"] = "rows 2, 6", ["Workbook"] = "rows 2, 3", ["ExcelWorkbook"] = "rows 2, 3",
        ["Worksheets"] = "row 3", ["ExcelWorksheets"] = "row 3", ["ExcelWorksheet"] = "row 3",
        ["Cells"] = "row 4", ["ExcelRange"] = "row 4", ["ExcelRangeBase"] = "row 4",
        ["Value"] = "row 5", ["Text"] = "row 5", ["GetValue"] = "row 5", ["SetValue"] = "row 5",
        ["Save"] = "row 6", ["SaveAs"] = "row 6", ["GetAsByteArray"] = "row 6", ["Stream"] = "row 6", ["File"] = "row 6",
        ["Dimension"] = "row 7", ["Start"] = "row 7", ["End"] = "row 7", ["ExcelAddressBase"] = "row 7", ["ExcelCellAddress"] = "row 7",
        ["Style"] = "row 8", ["ExcelStyle"] = "row 8", ["Font"] = "row 8", ["Fill"] = "row 8", ["Border"] = "row 8", ["Numberformat"] = "row 8",
        ["HorizontalAlignment"] = "row 8", ["VerticalAlignment"] = "row 8", ["WrapText"] = "row 8", ["ExcelFillStyle"] = "row 8",
        ["ExcelBorderStyle"] = "row 8", ["ExcelHorizontalAlignment"] = "row 8", ["ExcelVerticalAlignment"] = "row 8", ["SetColor"] = "row 8",
        ["AutoFitColumns"] = "row 9", ["Column"] = "row 9", ["Row"] = "row 9", ["Width"] = "row 9", ["Height"] = "row 9", ["Merge"] = "row 9",
        ["View"] = "row 9", ["FreezePanes"] = "row 9",
        ["LoadFromCollection"] = "row 10", ["LoadFromDataTable"] = "row 10", ["LoadFromArrays"] = "row 10", ["LoadFromText"] = "row 10",
        ["LicenseContext"] = "row 1", ["License"] = "row 1",
    };

    // "ExcelWorksheet", "ExcelRangeBase.Value", or null when the name does not bind to EPPlus.
    public static string? Name(SyntaxNode name, SemanticModel model)
    {
        SymbolInfo info = model.GetSymbolInfo(name);
        ISymbol? symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        if (symbol is null)
        {
            // E2: an EPPlus 5+ member does not resolve against 4.5.3.3; its receiver still does.
            return name.Parent is MemberAccessExpressionSyntax access && access.Name == name
                && model.GetTypeInfo(access.Expression).Type is INamedTypeSymbol receiver && IsEPPlus(receiver)
                ? $"{receiver.Name}.{((SimpleNameSyntax)name).Identifier.ValueText} (unresolved, EPPlus 5+)"
                : null;
        }

        return symbol switch
        {
            INamedTypeSymbol type when IsEPPlus(type) => type.Name,
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor when IsEPPlus(constructor.ContainingType) => constructor.ContainingType.Name,
            IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol when symbol.ContainingType is { } owner && IsEPPlus(owner) =>
                $"{owner.Name}.{symbol.Name}",
            _ => null,
        };
    }

    public static string Row(string api)
    {
        string key = api.Split(' ')[0];
        string member = key.Contains('.', StringComparison.Ordinal) ? key[(key.LastIndexOf('.') + 1)..] : key;
        string type = key.Split('.')[0];
        return s_rows.GetValueOrDefault(member) ?? s_rows.GetValueOrDefault(type) ?? "no row planned, by hand";
    }

    // The type, an array of it, or a generic argument names EPPlus.
    public static bool Mentions(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => Mentions(array.ElementType),
        INamedTypeSymbol named => IsEPPlus(named) || named.TypeArguments.Any(Mentions),
        _ => false,
    };

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
