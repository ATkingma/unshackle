using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// BeEquivalentTo(expected, o => o.Option()...): Shouldly's ShouldBeEquivalentTo has no options. Only options that change
// nothing for the shapes BeEquivalentToObjectPattern already handles are dropped, and WithStrictOrdering on a collection
// of value-compared items becomes ShouldBe(expected), which is ordered (probed against FluentAssertions 7.2.2 and
// Shouldly 4.3.0, TAAK-163). Every other option is Manual with its own reason. Options are read in order; the last
// WithStrictOrdering/WithoutStrictOrdering wins, as in FluentAssertions.
public class EquivalentOptionsPattern : BeEquivalentToObjectPattern
{
    public override string Id => "fa.equivalent-options";

    public override string Description => "x.Should().BeEquivalentTo(y, o => o.WithStrictOrdering()) on a collection of values → x.ShouldBe(y); no-op options are dropped; every other option is Manual";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEquivalentTo")
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingType.Name: not "StringAssertions" } method && HasOptions(method));

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        ArgumentSyntax? options = context.Values.FirstOrDefault(a => context.Model.GetTypeInfo(a.Expression).ConvertedType is INamedTypeSymbol { DelegateInvokeMethod.ReturnType.Name: "EquivalencyAssertionOptions" });
        IReadOnlyList<ArgumentSyntax> expected = [.. context.Values.Where(a => a != options)];
        if (options is null || expected.Count != 1)
        {
            return PatternResult.Manual("BeEquivalentTo(..., options) with an unexpected argument list: rewrite it by hand.");
        }

        if (!TryGetOptionCalls(options.Expression, out IReadOnlyList<InvocationExpressionSyntax> calls))
        {
            return PatternResult.Manual("The options are no inline lambda of the form `o => o.Option(...)`: a block body, a variable or a method group cannot be checked; Shouldly's ShouldBeEquivalentTo has no options.");
        }

        var reasons = new List<string>();
        bool strictOrdering = false;
        bool excludingMissingMembers = false;
        foreach (InvocationExpressionSyntax call in calls)
        {
            string option = ((MemberAccessExpressionSyntax)call.Expression).Name.Identifier.ValueText;
            switch (option)
            {
                case "WithStrictOrdering":
                    strictOrdering = true;
                    break;
                case "WithoutStrictOrdering":
                    strictOrdering = false;
                    break;
                // Both only matter when the expectation lacks a member of the subject; the same compile-time type has none.
                case "ExcludingMissingMembers":
                    excludingMissingMembers = true;
                    break;
                case "ThrowingOnMissingMembers":
                    break;
                default:
                    reasons.Add($"{option}{call.ArgumentList.ToString().ReplaceLineEndings(" ")}: {OptionReason(option)}");
                    break;
            }
        }

        if (reasons.Count > 0)
        {
            return PatternResult.Manual(string.Join(" ", reasons));
        }

        AssertionContext plain = new()
        {
            Node = context.Node,
            Assertion = context.Assertion,
            Name = context.Name,
            Subject = context.Subject,
            Model = context.Model,
            Values = expected,
            Because = context.Because,
        };
        ITypeSymbol? subjectType = context.Model.GetTypeInfo(context.Subject).Type;
        ITypeSymbol? expectedType = context.Model.GetTypeInfo(expected[0].Expression).Type;
        if (strictOrdering && subjectType is not null and not IErrorTypeSymbol && expectedType is not null and not IErrorTypeSymbol
            && ElementType(subjectType) is { } element)
        {
            if (!SymbolEqualityComparer.Default.Equals(element, ElementType(expectedType)))
            {
                return PatternResult.Manual("WithStrictOrdering() on collections with different item types: FluentAssertions compares the items by their members, Shouldly's ShouldBe by Equals.");
            }

            return IsValueLike(element)
                ? StrictOrdering(plain)
                : PatternResult.Manual($"WithStrictOrdering() on a collection of {element.Name}: FluentAssertions compares the items by their members, Shouldly's ShouldBe by Equals, which is reference equality for a class and not provably the same for this type.");
        }

        PatternResult result = base.RewriteAssertion(plain);
        return result is ManualResult { Reason: var reason } && excludingMissingMembers && reason.StartsWith("Different compile-time types", StringComparison.Ordinal)
            ? PatternResult.Manual("ExcludingMissingMembers() on different compile-time types: FluentAssertions compares only the members both types share, Shouldly requires equal types.")
            : result;
    }

    // ShouldBe(expected) is the ordered, Equals-based comparison; with a message the overload needs ignoreOrder (as in EqualSequencePattern).
    private static PatternResult StrictOrdering(AssertionContext context)
    {
        ArgumentSyntax expected = context.Values[0];
        return context.Because is null
            ? PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), "ShouldBe", expected))
            : PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
                context.Subject.WithoutTrailingTrivia(),
                "ShouldBe",
                expected,
                SyntaxFactory.Argument(SyntaxFactory.NameColon("ignoreOrder").WithTrailingTrivia(SyntaxFactory.Space), default, SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                context.Because));
    }

    // The option calls of `o => o.A().B()` in order, or false when the lambda has another shape.
    private static bool TryGetOptionCalls(ExpressionSyntax expression, out IReadOnlyList<InvocationExpressionSyntax> calls)
    {
        var list = new List<InvocationExpressionSyntax>();
        calls = list;
        (string? parameter, ExpressionSyntax? body) = expression switch
        {
            SimpleLambdaExpressionSyntax simple => (simple.Parameter.Identifier.ValueText, simple.ExpressionBody),
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [var single] } parenthesized => (single.Identifier.ValueText, parenthesized.ExpressionBody),
            _ => (null, null),
        };

        while (body is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation)
        {
            list.Insert(0, invocation);
            body = access.Expression;
        }

        return parameter is not null && list.Count > 0 && body is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == parameter;
    }

    // Items for which Equals equals FluentAssertions' member comparison: a leaf, or a record (class: sealed) or struct with only
    // leaf fields, each public or behind a public auto property, no computed public property and no hand-written Equals.
    private static bool IsValueLike(ITypeSymbol type)
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

    private static bool IsLeafOrNullable(ITypeSymbol type) =>
        IsLeaf(type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type);

    private static string OptionReason(string option) => option switch
    {
        "Excluding" or "ExcludingFields" or "ExcludingProperties" or "ExcludingNestedObjects" =>
            "ShouldBeEquivalentTo compares every member and cannot skip any; compare the remaining members one by one, or build the expectation without the excluded members.",
        "Including" or "IncludingAllDeclaredProperties" or "IncludingAllRuntimeProperties" or "IncludingFields" or "IncludingProperties"
            or "IncludingInternalFields" or "IncludingInternalProperties" or "IncludingNonBrowsableMembers" or "IgnoringNonBrowsableMembersOnSubject" or "WithoutSelectionRules" =>
            "it changes which members are compared; Shouldly has no member selection.",
        "WithMapping" or "WithoutMatchingRules" =>
            "it matches members by another name or rule; Shouldly matches members by name only.",
        "WithStrictOrderingFor" or "WithoutStrictOrderingFor" =>
            "it sets the item order per member or path; Shouldly's ShouldBeEquivalentTo has no ordering option.",
        "Using" or "When" or "WhenTypeIs" or "ComparingByMembers" or "ComparingByValue" or "ComparingRecordsByMembers" or "ComparingRecordsByValue"
            or "ComparingEnumsByName" or "ComparingEnumsByValue" or "WithAutoConversion" or "WithAutoConversionFor" or "WithoutAutoConversionFor" =>
            "it changes how values are compared (comparer, tolerance, type or enum rules); Shouldly's ShouldBeEquivalentTo has no comparison options (use ShouldBe with a tolerance or comparer for a single value).",
        "RespectingRuntimeTypes" or "RespectingDeclaredTypes" =>
            "it picks declared or runtime types for the members; Shouldly always compares runtime types.",
        "AllowingInfiniteRecursion" or "IgnoringCyclicReferences" =>
            "it controls cyclic object graphs; Shouldly has no such option.",
        "WithTracing" =>
            "it only adds a trace to the failure message; remove it by hand and the assertion is unchanged.",
        _ => "Shouldly's ShouldBeEquivalentTo has no options and this one is not known to be a no-op.",
    };
}
