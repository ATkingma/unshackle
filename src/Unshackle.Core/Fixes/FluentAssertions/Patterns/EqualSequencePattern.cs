using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class EqualSequencePattern : IPattern
{
    public string Id => "fa.equal-sequence";

    public string Description => "x.Should().Equal(seq) / Equal(1, 2) → x.ShouldBe(seq) / x.ShouldBe(new[] { 1, 2 }) (order-sensitive)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Equal", "NotEqual");

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

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And or a value); ShouldBe returns nothing.");
        }

        if (model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol { ContainingType.Name: "GenericCollectionAssertions" or "StringCollectionAssertions" } method)
        {
            return PatternResult.Manual($"{name}(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand.");
        }

        if (name == "NotEqual")
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
            return Fixed(subject, [SyntaxFactory.Argument(array)]);
        }

        if (method.Parameters.Any(p => p.Type is INamedTypeSymbol { DelegateInvokeMethod: not null }))
        {
            return PatternResult.Manual("Equal(...) with a predicate: pass an IEqualityComparer to ShouldBe by hand.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual("Equal(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        // The only sequence ShouldBe with a message is (expected, ignoreOrder, customMessage).
        return Fixed(subject, because is null
            ? [values[0]]
            : [values[0], SyntaxFactory.ParseArgumentList("(ignoreOrder: false)").Arguments[0], because]);
    }

    private static PatternResult Fixed(ExpressionSyntax subject, ArgumentSyntax[] arguments) =>
        PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, subject.WithoutTrailingTrivia(), SyntaxFactory.IdentifierName("ShouldBe")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                arguments.Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space))))));
}
