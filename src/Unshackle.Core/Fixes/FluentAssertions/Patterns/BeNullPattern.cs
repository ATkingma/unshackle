using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeNullPattern : IPattern
{
    public string Id => "fa.be-null";

    public string Description => "x.Should().BeNull() → x.ShouldBeNull(), NotBeNull() → ShouldNotBeNull()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeNull", "NotBeNull");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}() is not in the form x.Should().{name}(), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}() is used (.And/.Which or a value); Shouldly's ShouldBeNull returns nothing and ShouldNotBeNull returns the value itself.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out _, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        // The same extension works for reference types and Nullable<T>, so no type switch is needed.
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName(name == "BeNull" ? "ShouldBeNull" : "ShouldNotBeNull")),
            SyntaxFactory.ArgumentList(because is null
                ? default
                : SyntaxFactory.SingletonSeparatedList(because))));
    }
}
