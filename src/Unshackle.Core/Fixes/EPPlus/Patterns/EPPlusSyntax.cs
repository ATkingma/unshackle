using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// Recognises EPPlus types by symbol, or by name when the symbol does not resolve: council round 8, E2 (5+ members are matched by
// syntax because the test types, and many real projects' semantic models, are 4.5.3.3).
internal static class EPPlusSyntax
{
    public const string Namespace = "OfficeOpenXml";

    public const string PackageType = "ExcelPackage";

    // True when the expression names the ExcelPackage type (ExcelPackage, OfficeOpenXml.ExcelPackage, global::...), not an own type of that name.
    public static bool IsPackageType(ExpressionSyntax expression, SemanticModel model)
    {
        string? name = expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            _ => null,
        };
        if (name != PackageType)
        {
            return false;
        }

        ISymbol? symbol = model.GetSymbolInfo(expression).Symbol;
        return symbol is null ? expression.ToString().Replace("global::", "", StringComparison.Ordinal) is PackageType or $"{Namespace}.{PackageType}"
            : IsPackageType(symbol as ITypeSymbol);
    }

    public static bool IsPackageType(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: PackageType, ContainingNamespace: { Name: Namespace, ContainingNamespace.IsGlobalNamespace: true } };
}
