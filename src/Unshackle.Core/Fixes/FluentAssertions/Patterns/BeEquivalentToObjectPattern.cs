using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// FluentAssertions' BeEquivalentTo is lenient (any order, Equals overrides, declared types, extra runtime members);
// Shouldly's ShouldBeEquivalentTo is strict. Only shapes where both decide the same are Fixed: the same plain class or
// struct on both sides, with leaf or plain nested members. Shouldly also compares runtime types: a non-sealed type
// (unless both sides are new-expressions) is Fixed with a warning in the report. Shouldly passes where FluentAssertions fails for Equals overrides,
// cyclic graphs and types without public members, so those are Manual. See fa-be-equivalent-to-object-modellen.md.
public class BeEquivalentToObjectPattern : IPattern
{
    public string Id => "fa.be-equivalent-to-object";

    public string Description => "obj.Should().BeEquivalentTo(other) → obj.ShouldBeEquivalentTo(other) for the same plain type, otherwise Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEquivalentTo", "NotBeEquivalentTo")
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingType.Name: not "StringAssertions" });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        if (name == "NotBeEquivalentTo")
        {
            return PatternResult.Manual("NotBeEquivalentTo(...) has no Shouldly equivalent: there is no ShouldNotBeEquivalentTo.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And/.Which or a value); Shouldly's ShouldBeEquivalentTo returns nothing.");
        }

        // BeEquivalentTo(params TExpectation[]) on a collection: the extra arguments are expected items, not becauseArgs.
        if (model.GetSymbolInfo(assertion).Symbol is IMethodSymbol { Parameters: [{ IsParams: true }, ..] })
        {
            return PatternResult.Manual($"{name}(params items) is a collection assertion: FluentAssertions ignores the item order by default, Shouldly does not.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

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
        ArgumentSyntax[] arguments = because is null ? [values[0]] : [values[0], because.WithLeadingTrivia(SyntaxFactory.Space)];
        InvocationExpressionSyntax rewritten = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("ShouldBeEquivalentTo")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
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

    private static string DerivableWarning(IEnumerable<string> types) =>
        $"ShouldBeEquivalentTo fails when the runtime type differs; FluentAssertions accepted derived types (not sealed: {string.Join(", ", types.Distinct())}).";

    // Compared by value in the same way by both libraries (probed): primitives, string, decimal, enums and a few BCL structs.
    private static bool IsLeaf(ITypeSymbol t) =>
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
