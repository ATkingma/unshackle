using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// ContainEquivalentOf compares the item with every element by members (strings ordinal and case-sensitive, like Equals).
// ShouldContain uses Equals, so only items where both agree are Fixed (probed against FluentAssertions 7.2.2 and
// Shouldly 4.3.0, TAAK-164): the element type is a leaf or a value-like record/struct (IsValueLike) and the expectation has
// that same type. A class, a different expectation type (FluentAssertions converts 2L to int, Shouldly does not compile),
// equivalency options and dictionaries are Manual. See fa-contain-equivalent-of-modellen.md.
public class ContainEquivalentOfPattern : BeEquivalentToObjectPattern
{
    public override string Id => "fa.contain-equivalent-of";

    public override string Description => "x.Should().ContainEquivalentOf(item) → x.ShouldContain(item), NotContainEquivalentOf → ShouldNotContain, for value-compared items of the element type; otherwise Manual";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "ContainEquivalentOf", "NotContainEquivalentOf");

    // BeEquivalentToObjectPattern checks params and NotBeEquivalentTo here; neither applies to this assertion.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context) => null;

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        string name = context.Name;
        if (context.Values.Count != 1)
        {
            return PatternResult.Manual($"{name}(item, options => ...): equivalency options have no Shouldly equivalent; ShouldContain compares with Equals.");
        }

        ITypeSymbol? subjectType = model.GetTypeInfo(context.Subject).Type;
        ITypeSymbol? expectedType = model.GetTypeInfo(context.Values[0].Expression).Type;
        if (subjectType is null or IErrorTypeSymbol || expectedType is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject or the item is unknown (null literal or unresolved).");
        }

        if (ElementType(subjectType) is not { } element)
        {
            return PatternResult.Manual("The subject is not a generic IEnumerable<T>.");
        }

        if (!SymbolEqualityComparer.Default.Equals(element, expectedType))
        {
            return PatternResult.Manual($"The item is a {expectedType.Name} but the elements are {element.Name}: FluentAssertions converts or compares by members, Shouldly's ShouldContain needs the element type.");
        }

        if (!IsValueLike(element))
        {
            return PatternResult.Manual($"{element.Name} is no leaf, record or struct of leaf members: FluentAssertions compares the items by their members, ShouldContain by Equals (reference equality for a class, and not provably the same for this type).");
        }

        string shouldName = name == "ContainEquivalentOf" ? "ShouldContain" : "ShouldNotContain";
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            FluentAssertionsSyntax.NotNull(context.Subject, model).WithoutTrailingTrivia(), shouldName, context.Values[0], context.Because));
    }
}
