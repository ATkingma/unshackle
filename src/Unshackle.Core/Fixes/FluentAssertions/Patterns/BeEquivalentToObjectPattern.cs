using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
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

    // Before the argument split: BeEquivalentTo(params string[]) on a collection takes expected items, not becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        if (context.Name == "NotBeEquivalentTo")
        {
            return PatternResult.Manual("NotBeEquivalentTo(...) has no Shouldly equivalent: there is no ShouldNotBeEquivalentTo.");
        }

        // An array passed as the argument is an ordinary expectation (RewriteAssertion); only the expanded form BeEquivalentTo("a", "b") is handled here.
        if (context.Model.GetOperation(context.Assertion) is not IInvocationOperation { Arguments: [{ ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation { Type: IArrayTypeSymbol arrayType } }] })
        {
            return null;
        }

        if (context.Assertion.ArgumentList.Arguments.Any(a => a.NameColon is not null))
        {
            return PatternResult.Manual($"{context.Name}(...) with named arguments: rewrite it by hand.");
        }

        // Same value-compared items as the collection case below (probed: reorder, duplicates, different length, null subject), written as the array FluentAssertions builds.
        ITypeSymbol? subjectType = context.Model.GetTypeInfo(context.Subject).Type;
        if (subjectType is null or IErrorTypeSymbol || ElementType(subjectType) is not { } element
            || !SymbolEqualityComparer.Default.Equals(element, arrayType.ElementType) || !IsValueLike(element))
        {
            return PatternResult.Manual($"{context.Name}(params items) is a collection assertion on items that are no leaf, record or struct of leaf members, or of another type than the subject's elements: FluentAssertions ignores the item order by default, Shouldly does not.");
        }

        ExpressionSyntax array = ParamsArraySyntax.Create(context.Assertion, context.Assertion.ArgumentList.Arguments.Select(a => a.Expression), arrayType, context.Model);
        ArgumentSyntax ignoreOrder = IgnoreOrder();
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), "ShouldBe", SyntaxFactory.Argument(array), ignoreOrder));
    }

    private static ArgumentSyntax IgnoreOrder() =>
        SyntaxFactory.Argument(SyntaxFactory.NameColon("ignoreOrder").WithTrailingTrivia(SyntaxFactory.Space), default, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

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

        // A collection of value-compared items (leaf, or a record/struct of leaves: IsValueLike): FA matches each item once in any
        // order, like ShouldBe(ignoreOrder: true) (probed: duplicates, null items, empty, different lengths, null subject or
        // expectation). TAAK-092, TAAK-172.
        if (ElementType(subjectType) is { } element && IsValueLike(element)
            && SymbolEqualityComparer.Default.Equals(element, ElementType(expectedType)))
        {
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(subject.WithoutTrailingTrivia(), "ShouldBe", values[0], IgnoreOrder(), because));
        }

        if (ElementType(subjectType) is { } items && ElementType(expectedType) is not null && subjectType.SpecialType != SpecialType.System_String)
        {
            return PatternResult.Manual(IsValueLike(items)
                ? $"The expectation has other items than the subject's {items.Name}: FluentAssertions converts or compares them by members, ShouldBe(ignoreOrder: true) needs the same element type."
                : $"The items are {items.Name}, no leaf, record or struct of leaf members: FluentAssertions compares them by members in any order, Shouldly's ShouldBe(ignoreOrder: true) by Equals.");
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

    // Items for which Equals equals FluentAssertions' member comparison: a leaf, or a record (class: sealed) or struct with only
    // leaf fields, each public or behind a public auto property, no computed public property and no hand-written Equals.
    protected internal static bool IsValueLike(ITypeSymbol type)
    {
        ITypeSymbol item = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;
        if (IsLeaf(item))
        {
            return true;
        }

        if (item is not INamedTypeSymbol { IsAbstract: false, IsRefLikeType: false, IsAnonymousType: false, SpecialType: SpecialType.None } named
            || named.ContainingNamespace.ToDisplayString().StartsWith("System", StringComparison.Ordinal)
            || !(named.TypeKind == TypeKind.Struct || named is { TypeKind: TypeKind.Class, IsRecord: true, IsSealed: true, BaseType.SpecialType: SpecialType.System_Object }))
        {
            return false;
        }

        System.Collections.Immutable.ImmutableArray<ISymbol> members = named.GetMembers();
        var fields = members.OfType<IFieldSymbol>().Where(f => f is { IsStatic: false, IsConst: false }).ToList();
        return fields.Count > 0
            && !members.Any(m => m.Name == "Equals" && !m.IsImplicitlyDeclared)
            && fields.All(f => IsLeafOrNullable(f.Type) && (f.DeclaredAccessibility == Accessibility.Public || f.AssociatedSymbol is IPropertySymbol { DeclaredAccessibility: Accessibility.Public }))
            && members.OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, IsIndexer: false, IsImplicitlyDeclared: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null })
                .All(p => fields.Any(f => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, p)));
    }

    protected static bool IsLeafOrNullable(ITypeSymbol type) =>
        IsLeaf(type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type);

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
