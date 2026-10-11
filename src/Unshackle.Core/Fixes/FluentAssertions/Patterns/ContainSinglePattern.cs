using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainSinglePattern : FluentAssertionPattern
{
    public override string Id => "fa.contain-single-which";

    public override string Description => "x.Should().ContainSingle() → x.ShouldHaveSingleItem(), ContainSingle(p) → x.ShouldContain(p, 1), var y = ….Which → var y = x.Where(p).ShouldHaveSingleItem()";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "ContainSingle").Select(FluentAssertionsSyntax.WithWhich);

    // `var item = x.Should().ContainSingle().Which;` (or .Subject): ShouldHaveSingleItem returns the item itself.
    protected override InvocationExpressionSyntax GetAssertion(SyntaxNode node) => FluentAssertionsSyntax.AssertionOfWhich(node);

    protected override bool IsResultDiscarded(AssertionContext context) => FluentAssertionsSyntax.IsResultDiscardedOrWhich(context);

    protected override string ResultUsedReason(string name) => FluentAssertionsSyntax.WhichUsedReason(name);

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        if (context.Model.GetTypeInfo(context.Subject).Type is not { } subjectType || !subjectType.AllInterfaces.Prepend(subjectType).Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T))
        {
            return PatternResult.Manual("ContainSingle(...) on a non-generic collection: Shouldly needs IEnumerable<T>.");
        }

        ExpressionSyntax receiver = FluentAssertionsSyntax.NotNull(context.Subject, context.Model).WithoutTrailingTrivia();
        if (context.Values.Count == 0)
        {
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(receiver, "ShouldHaveSingleItem", context.Because));
        }

        ArgumentSyntax predicate = context.Values[0];
        if (context.Node is not MemberAccessExpressionSyntax)
        {
            // ShouldContain(predicate, 1) takes the same Expression<Func<T, bool>> and also fails on zero or several matches.
            ArgumentSyntax once = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(receiver, "ShouldContain", predicate, once, context.Because));
        }

        if (predicate.Expression is not AnonymousFunctionExpressionSyntax)
        {
            return PatternResult.Manual("ContainSingle(predicate).Which with an Expression variable: Where needs a Func; rewrite it by hand.");
        }

        return PatternResult.Fixed(
            FluentAssertionsSyntax.Invoke(FluentAssertionsSyntax.Invoke(receiver, "Where", predicate), "ShouldHaveSingleItem", context.Because), "System.Linq");
    }
}
