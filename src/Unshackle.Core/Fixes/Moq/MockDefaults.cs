using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq;

// Where an unconfigured Moq mock and an NSubstitute substitute of the same interface answer differently (council round 9, Q1).
//   new Mock<T>(): DefaultValue.Empty (null for string, interfaces, delegates), properties do not remember a set value.
//   Mock.Of<T>(): DefaultValue.Mock (a mock for every mockable type, also non-sealed classes) and all properties stubbed.
//   NSubstitute: "" for string, a substitute for interfaces, delegates and pure-virtual classes, properties remember.
// Both give empty arrays, empty IEnumerable/IQueryable, completed tasks and default value types.
// A known difference (string, interface or delegate → Moq's null, also inside Task<T>/ValueTask<T>) is pinned with ReturnsNull();
// any other difference, or a settable property, blocks the rewrite.
public static class MockDefaults
{
    public const int MaxPins = 10;

    // setUp: members the test sets up for every argument; they get no pin and do not count. tracked / allTracked: properties that remember
    // a set value in Moq too (SetupProperty / SetupAllProperties); their setter is no difference.
    public static DefaultsPlan Plan(ITypeSymbol mocked, bool mockOf, IEnumerable<ISymbol> setUp, IEnumerable<ISymbol> tracked, bool allTracked)
    {
        var exempt = new HashSet<ISymbol>(setUp, SymbolEqualityComparer.Default);
        var remembers = new HashSet<ISymbol>(tracked, SymbolEqualityComparer.Default);
        var pins = new List<ISymbol>();
        foreach (ITypeSymbol owner in (IEnumerable<ITypeSymbol>)[mocked, .. mocked.AllInterfaces])
        {
            foreach (ISymbol member in owner.GetMembers().Where(m => !m.IsStatic))
            {
                string? blocker = member switch
                {
                    IPropertySymbol { SetMethod: not null } p when !mockOf && !allTracked && !remembers.Contains(p) =>
                        $"{p.Name} has a setter (NSubstitute remembers a set value, a Moq mock does not)",
                    IPropertySymbol { GetMethod: not null } p => Classify(p.Type, mockOf) switch
                    {
                        Kind.Same => null,
                        Kind.Pin when !p.IsIndexer => Pin(p, exempt, pins),
                        _ => $"{p.Name} returns {p.Type.ToDisplayString()}, whose default differs in a way that cannot be pinned",
                    },
                    IMethodSymbol { MethodKind: MethodKind.Ordinary } m => Classify(m.ReturnType, mockOf) switch
                    {
                        Kind.Same => null,
                        Kind.Pin when !m.IsGenericMethod && m.Parameters.All(a => a.RefKind == RefKind.None && !a.IsParams) => Pin(m, exempt, pins),
                        _ => $"{m.Name} returns {m.ReturnType.ToDisplayString()}, whose default differs in a way that cannot be pinned",
                    },
                    _ => null,
                };
                if (blocker is not null)
                {
                    return new() { Blocker = MoqBlocker.Of(MoqReason.DefaultValueDiffers, "an unconfigured " + mocked.Name + " answers differently in NSubstitute: " + blocker) };
                }
            }
        }

        return pins.Count > MaxPins
            ? new() { Blocker = MoqBlocker.Of(MoqReason.TooManyDefaultPins, $"{pins.Count} members of {mocked.Name} would need Moq's default pinned (max {MaxPins})") }
            : new() { Pins = pins };
    }

    private static string? Pin(ISymbol member, HashSet<ISymbol> exempt, List<ISymbol> pins)
    {
        if (!exempt.Contains(member))
        {
            pins.Add(member);
        }

        return null;
    }

    private enum Kind
    {
        Same,
        Pin,
        Unknown,
    }

    private static Kind Classify(ITypeSymbol type, bool mockOf)
    {
        if (type.SpecialType == SpecialType.System_Void || type is IArrayTypeSymbol)
        {
            return Kind.Same;
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return Kind.Pin;
        }

        if (type is not INamedTypeSymbol named)
        {
            return Kind.Unknown;
        }

        string definition = named.OriginalDefinition.ToDisplayString();
        if (definition is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>")
        {
            return Classify(named.TypeArguments[0], mockOf);
        }

        if (named.IsValueType || definition is "System.Threading.Tasks.Task" or "System.Collections.IEnumerable"
            or "System.Collections.Generic.IEnumerable<T>" or "System.Linq.IQueryable" or "System.Linq.IQueryable<T>")
        {
            return Kind.Same;
        }

        if (named.TypeKind is TypeKind.Interface or TypeKind.Delegate)
        {
            // Mock.Of answers with a recursive mock, not null: no known pin.
            return mockOf ? Kind.Unknown : Kind.Pin;
        }

        if (mockOf)
        {
            return named.IsSealed ? Kind.Same : Kind.Unknown;
        }

        return IsPureVirtual(named) ? Kind.Unknown : Kind.Same;
    }

    // NSubstitute's AutoSubstituteProvider: not sealed, a public parameterless constructor, every public member virtual or abstract.
    private static bool IsPureVirtual(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsSealed: false }
        && type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)
        && type.GetMembers().Where(m => m is { IsStatic: false, DeclaredAccessibility: Accessibility.Public }
                and (IMethodSymbol { MethodKind: MethodKind.Ordinary } or IPropertySymbol))
            .All(m => m.IsVirtual || m.IsAbstract || m.IsOverride);
}
