using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// FluentAssertions' BeEquivalentTo is lenient (any order, Equals overrides, declared types, extra runtime members);
// Shouldly's ShouldBeEquivalentTo is strict. Only shapes where both decide the same are Fixed: the same plain class or
// struct on both sides, with leaf or plain nested members. Shouldly also compares runtime types: a non-sealed type
// (unless both sides are new-expressions) is Fixed with a warning in the report. Shouldly passes where FluentAssertions fails for Equals overrides,
// cyclic graphs and types without public members, so those are Manual. See fa-be-equivalent-to-object-modellen.md.
public class BeEquivalentToObjectPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-equivalent-to-object";

    public override string Description => "obj.Should().BeEquivalentTo(other) → obj.ShouldBeEquivalentTo(other) for the same plain type; a collection of primitives → ShouldBe(other, ignoreOrder: true); otherwise Manual";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEquivalentTo", "NotBeEquivalentTo")
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingType.Name: not "StringAssertions" } method
                && !(i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "BeEquivalentTo" } && HasOptions(method)));

    // BeEquivalentTo(expected, options => ...): the overloads with a Func<EquivalencyAssertionOptions<T>, ...>; EquivalentOptionsPattern owns those.
    protected static bool HasOptions(IMethodSymbol method) =>
        method.Parameters.Any(p => p.Type is INamedTypeSymbol { DelegateInvokeMethod.ReturnType.Name: "EquivalencyAssertionOptions" });

    // Before the argument split: BeEquivalentTo(params TExpectation[]) on a collection takes expected items, not becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context) =>
        context.Name == "NotBeEquivalentTo"
            ? PatternResult.Manual("NotBeEquivalentTo(...) has no Shouldly equivalent: there is no ShouldNotBeEquivalentTo.")
            : context.Model.GetSymbolInfo(context.Assertion).Symbol is IMethodSymbol { Parameters: [{ IsParams: true }, ..] }
                ? PatternResult.Manual($"{context.Name}(params items) is a collection assertion: FluentAssertions ignores the item order by default, Shouldly does not.")
                : null;

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        string name = context.Name;
        ExpressionSyntax subject = context.Subject;
        IReadOnlyList<ArgumentSyntax> values = context.Values;
        ArgumentSyntax? because = context.Because;
        if (values.Count != 1)
        {
            return PatternResult.Manual($"{name}(..., options => ...) configuration (ordering, exclusions, tolerances) has no Shouldly equivalent.");
        }

        ITypeSymbol? subjectType = model.GetTypeInfo(subject).Type;
        ITypeSymbol? expectedType = model.GetTypeInfo(values[0].Expression).Type;
        if (subjectType is null or IErrorTypeSymbol || expectedType is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject or the expectation is unknown (null literal or unresolved).");
        }

        // A collection of value-compared leaves: FA matches each item once in any order, like ShouldBe(ignoreOrder: true)
        // (probed: duplicates, null items, empty, different lengths, null subject or expectation). TAAK-092.
        if (ElementType(subjectType) is { } element && IsLeaf(element is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } n ? n.TypeArguments[0] : element)
            && SymbolEqualityComparer.Default.Equals(element, ElementType(expectedType)))
        {
            ArgumentSyntax ignoreOrder = SyntaxFactory.Argument(SyntaxFactory.NameColon("ignoreOrder").WithTrailingTrivia(SyntaxFactory.Space), default, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(subject.WithoutTrailingTrivia(), "ShouldBe", values[0], ignoreOrder, because));
        }

        if (!SymbolEqualityComparer.Default.Equals(subjectType, expectedType))
        {
            return PatternResult.Manual("Different compile-time types (or an anonymous expectation): FluentAssertions compares only the expectation's members, Shouldly requires equal types.");
        }

        var derivable = new List<string>();
        if (Unsupported(subjectType, requireMembers: true, derivable) is { } reason)
        {
            return PatternResult.Manual(reason);
        }

        // Shouldly compares runtime types (also nested), FluentAssertions only the declared members: a derived instance
        // passes FA and fails Shouldly. Fixed anyway (user decision TAAK-049), with a warning unless no type can derive.
        if (subjectType is { TypeKind: TypeKind.Class, IsSealed: false }
            && !(subject is BaseObjectCreationExpressionSyntax && values[0].Expression is BaseObjectCreationExpressionSyntax))
        {
            derivable.Insert(0, subjectType.Name);
        }

        // The overload is ShouldBeEquivalentTo(expected, customMessage).
        InvocationExpressionSyntax rewritten = FluentAssertionsSyntax.Invoke(subject.WithoutTrailingTrivia(), "ShouldBeEquivalentTo", values[0], because);
        return new FixedResult([rewritten], [])
        {
            Warning = derivable.Count == 0 ? null : DerivableWarning(derivable),
        };
    }

    // The reason the type (and everything reachable through its public members) cannot be compared identically, or null.
    private static string? Unsupported(ITypeSymbol type, bool requireMembers, List<string> derivable, ISet<ITypeSymbol>? path = null)
    {
        if (type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable))
        {
            return $"{type.Name} is a collection: FluentAssertions ignores the item order by default, Shouldly does not.";
        }

        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false, IsAnonymousType: false, SpecialType: SpecialType.None } named
            || named.ContainingNamespace.ToDisplayString().StartsWith("System", StringComparison.Ordinal))
        {
            return $"{type.Name} is not a plain class or struct (object, interface, abstract, delegate or BCL type): its runtime members decide the comparison.";
        }

        path ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        if (!path.Add(named))
        {
            return $"{type.Name} is part of a cyclic object graph: FluentAssertions fails on it, Shouldly passes.";
        }

        if (HasEqualsOverride(named))
        {
            return $"{type.Name} overrides Equals: FluentAssertions compares it with Equals instead of its members.";
        }

        if (!requireMembers && named is { TypeKind: TypeKind.Class, IsSealed: false })
        {
            derivable.Add(named.Name);
        }

        var members = new List<ITypeSymbol>();
        for (INamedTypeSymbol? t = named; t is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) }; t = t.BaseType)
        {
            members.AddRange(t.GetMembers().Where(m => m is { DeclaredAccessibility: Accessibility.Public, IsStatic: false })
                .Select(m => m switch
                {
                    IPropertySymbol { IsIndexer: false, GetMethod: not null } p => p.Type,
                    IFieldSymbol { IsImplicitlyDeclared: false } f => f.Type,
                    _ => null,
                })
                .OfType<ITypeSymbol>());
        }

        if (requireMembers && members.Count == 0)
        {
            return $"{type.Name} has no public members: FluentAssertions throws, Shouldly passes.";
        }

        foreach (ITypeSymbol member in members)
        {
            ITypeSymbol leaf = member is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : member;
            if (!IsLeaf(leaf) && Unsupported(member, requireMembers: false, derivable, path) is { } reason)
            {
                return reason;
            }
        }

        path.Remove(named);
        return null;
    }

    // T of the one IEnumerable<T> the type is or implements, else null.
    protected static ITypeSymbol? ElementType(ITypeSymbol type) =>
        type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>()
            .Where(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            .Select(i => i.TypeArguments[0])
            .Distinct(SymbolEqualityComparer.Default)
            .ToList() is [ITypeSymbol single] ? single : null;

    private static string DerivableWarning(IEnumerable<string> types) =>
        $"ShouldBeEquivalentTo fails when the runtime type differs; FluentAssertions accepted derived types (not sealed: {string.Join(", ", types.Distinct())}).";

    // Compared by value in the same way by both libraries (probed): primitives, string, decimal, enums and a few BCL structs.
    protected static bool IsLeaf(ITypeSymbol t) =>
        t.TypeKind == TypeKind.Enum
        || t.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte
            or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
            or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal or SpecialType.System_Single
            or SpecialType.System_Double or SpecialType.System_String or SpecialType.System_DateTime
        || t.ToDisplayString() is "System.DateTimeOffset" or "System.Guid" or "System.TimeSpan" or "System.DateOnly" or "System.TimeOnly";

    // A user-written Equals(object); the one a record synthesizes is implicit and FluentAssertions compares records by members.
    private static bool HasEqualsOverride(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? t = type; t is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) }; t = t.BaseType)
        {
            if (t.GetMembers("Equals").OfType<IMethodSymbol>().Any(m => m is { IsOverride: true, IsImplicitlyDeclared: false, Parameters.Length: 1 }))
            {
                return true;
            }
        }

        return false;
    }
}
