using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

// The calls and element types of LoadFromPattern (queue row 10).
internal static class LoadFromMapping
{
    private static readonly HashSet<string> s_methods = new(["LoadFromCollection", "LoadFromDataTable", "LoadFromArrays", "LoadFromText", "LoadFromDataReader"], StringComparer.Ordinal);

    // Property types the harness proves (LoadFromHarnessTests), nullable forms included.
    private static readonly HashSet<SpecialType> s_covered =
        [SpecialType.System_String, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_Double, SpecialType.System_Boolean, SpecialType.System_Decimal];

    // The EPPlus load method a call binds to (on a range), or null.
    public static IMethodSymbol? Method(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } && s_methods.Contains(name)
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && IsEPPlus(method.ContainingType)
            ? method
            : null;

    // The first public member of the element type that the harness does not cover ("Order.Placed (DateTime)"), or null when all are
    // covered. A public field counts as uncovered: the two libraries do not read fields the same way.
    public static string? UncoveredMember(ITypeSymbol element)
    {
        if (element is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } type || type.SpecialType != SpecialType.None)
        {
            return $"{element.ToDisplayString()} (not a class or struct with properties)";
        }

        List<ISymbol> members = [.. type.GetMembers().Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic && m is IPropertySymbol or IFieldSymbol)];
        if (members.Count == 0 || type.BaseType is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) })
        {
            return $"{type.Name} (no own public properties, or a base class)";
        }

        foreach (ISymbol member in members)
        {
            ITypeSymbol? memberType = (member as IPropertySymbol)?.Type;
            if (member is IFieldSymbol || member is IPropertySymbol { IsIndexer: true } || member is IPropertySymbol { GetMethod: null } || !IsCovered(memberType!))
            {
                return $"{type.Name}.{member.Name} ({(member as IPropertySymbol)?.Type.ToDisplayString() ?? "field"})";
            }
        }

        return null;
    }

    private static bool IsCovered(ITypeSymbol type) =>
        s_covered.Contains(type.SpecialType)
        || (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable && s_covered.Contains(nullable.TypeArguments[0].SpecialType));

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
