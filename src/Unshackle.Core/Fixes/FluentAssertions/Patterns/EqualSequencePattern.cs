using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class EqualSequencePattern : FluentAssertionPattern
{
    public override string Id => "fa.equal-sequence";

    public override string Description => "x.Should().Equal(seq) / Equal(1, 2) → x.ShouldBe(seq) / x.ShouldBe(new[] { 1, 2 }) (order-sensitive)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Equal", "NotEqual");

    // Before the argument split: Equal(params T[] elements) would be reported as becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;
        if (model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol { ContainingType.Name: "GenericCollectionAssertions" or "StringCollectionAssertions" } method)
        {
            return PatternResult.Manual($"{context.Name}(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand.");
        }

        if (context.Name == "NotEqual")
        {
            return PatternResult.Manual("NotEqual(...): Shouldly has no ShouldNotBe for sequences; e.g. x.SequenceEqual(y).ShouldBeFalse().");
        }

        // Equal(params T[] elements): build the array FluentAssertions builds, typed so ShouldBe infers the element type.
        if (method.Parameters is [{ IsParams: true, Type: IArrayTypeSymbol arrayType }]
            && assertion.ArgumentList.Arguments.All(a => a.NameColon is null)
            && !(assertion.ArgumentList.Arguments.Count == 1
                && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(assertion.ArgumentList.Arguments[0].Expression).ConvertedType, arrayType)
                && model.GetTypeInfo(assertion.ArgumentList.Arguments[0].Expression).Type is IArrayTypeSymbol))
        {
            string elements = string.Join(", ", assertion.ArgumentList.Arguments.Select(a => a.Expression.WithoutTrivia().ToString()));
            ExpressionSyntax array = SyntaxFactory.ParseExpression(
                $"new {arrayType.ToMinimalDisplayString(model, assertion.SpanStart)} {{ {elements}{(elements.Length == 0 ? "" : " ")}}}");
            return Fixed(context, SyntaxFactory.Argument(array));
        }

        if (method.Parameters.Any(p => p.Type is INamedTypeSymbol { DelegateInvokeMethod: not null }))
        {
            return PatternResult.Manual("Equal(...) with a predicate: pass an IEqualityComparer to ShouldBe by hand.");
        }

        return null;
    }

    // The only sequence ShouldBe with a message is (expected, ignoreOrder, customMessage).
    protected override PatternResult RewriteAssertion(AssertionContext context) =>
        context.Because is null
            ? Fixed(context, context.Values[0])
            : Fixed(context, context.Values[0], SyntaxFactory.ParseArgumentList("(ignoreOrder: false)").Arguments[0], context.Because);

    private static PatternResult Fixed(AssertionContext context, params IEnumerable<ArgumentSyntax> arguments) =>
        PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), "ShouldBe", arguments));
}
