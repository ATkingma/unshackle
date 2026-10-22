using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq;

// Where an unconfigured Moq mock and an NSubstitute substitute of the same interface answer differently. A test that
// passes the mock without setups still lets the code under test call it, so any such member makes the rewrite unproven.
//   new Mock<T>(): DefaultValue.Empty (null for string, interfaces, delegates), properties do not remember a set value.
//   Mock.Of<T>(): DefaultValue.Mock (a mock for every mockable type, also non-sealed classes) and all properties stubbed.
//   NSubstitute: "" for string, a substitute for interfaces, delegates and pure-virtual classes, properties remember.
// Both give empty arrays, empty IEnumerable/IQueryable and completed tasks.
public static class MockDefaults
{
    public static string? Difference(ITypeSymbol mocked, bool mockOf) =>
        MembersDifference(mocked, mockOf, settersStubbed: mockOf, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));

    private static string? MembersDifference(ITypeSymbol type, bool mockOf, bool settersStubbed, HashSet<ITypeSymbol> visited)
    {
        if (!visited.Add(type))
        {
            return null;
        }

        foreach (ITypeSymbol owner in (IEnumerable<ITypeSymbol>)[type, .. type.AllInterfaces])
        {
            foreach (ISymbol member in owner.GetMembers())
            {
                string? difference = member switch
                {
                    IPropertySymbol { IsStatic: false } p when p.SetMethod is not null && !settersStubbed =>
                        $"{p.Name} has a setter (NSubstitute remembers a set value, a Moq mock does not)",
                    IPropertySymbol { IsStatic: false } p when p.GetMethod is not null => Returned(p.Type, mockOf, visited) is { } d ? $"{p.Name} returns {d}" : null,
                    IMethodSymbol { IsStatic: false, MethodKind: MethodKind.Ordinary } m =>
                        Returned(m.ReturnType, mockOf, visited) is { } d ? $"{m.Name} returns {d}" : null,
                    _ => null,
                };
                if (difference is not null)
                {
                    return difference;
                }
            }
        }

        return null;
    }

    private static string? Returned(ITypeSymbol type, bool mockOf, HashSet<ITypeSymbol> visited)
    {
        if (type.SpecialType == SpecialType.System_Void || type is IArrayTypeSymbol)
        {
            return null;
        }

        if (type is ITypeParameterSymbol)
        {
            return "a type parameter (its default differs per type)";
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return "string (Moq: null, NSubstitute: \"\")";
        }

        if (type is not INamedTypeSymbol named)
        {
            return $"{type.ToDisplayString()} (no proven default)";
        }

        string definition = named.OriginalDefinition.ToDisplayString();
        if (definition is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>")
        {
            return Returned(named.TypeArguments[0], mockOf, visited);
        }

        if (named.IsValueType || definition is "System.Threading.Tasks.Task" or "System.Collections.IEnumerable"
            or "System.Collections.Generic.IEnumerable<T>" or "System.Linq.IQueryable" or "System.Linq.IQueryable<T>")
        {
            return null;
        }

        if (named.TypeKind is TypeKind.Interface or TypeKind.Delegate)
        {
            // Mock.Of: both return a recursive mock, whose own members must agree too (Moq's inner mocks do not stub properties).
            return mockOf ? MembersDifference(named, mockOf, settersStubbed: false, visited)
                : $"{named.ToDisplayString()} (Moq: null, NSubstitute: a substitute)";
        }

        if (mockOf)
        {
            return named.IsSealed ? null : $"{named.ToDisplayString()} (Moq: a mock, NSubstitute: null unless the class is pure virtual)";
        }

        return IsPureVirtual(named) ? $"{named.ToDisplayString()} (Moq: null, NSubstitute: a substitute)" : null;
    }

    // NSubstitute's AutoSubstituteProvider: not sealed, a public parameterless constructor, every public member virtual or abstract.
    private static bool IsPureVirtual(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsSealed: false }
        && type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)
        && type.GetMembers().Where(m => m is { IsStatic: false, DeclaredAccessibility: Accessibility.Public }
                and (IMethodSymbol { MethodKind: MethodKind.Ordinary } or IPropertySymbol))
            .All(m => m.IsVirtual || m.IsAbstract || m.IsOverride);
}
