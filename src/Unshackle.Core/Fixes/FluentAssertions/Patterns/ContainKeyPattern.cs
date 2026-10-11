using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainKeyPattern : IPattern
{
    public string Id => "fa.contain-key";

    public string Description => "dict.Should().ContainKey(k) → dict.ShouldContainKey(k), NotContainKey → ShouldNotContainKey";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(
            root, model, "ContainKey", "NotContainKey", "ContainKeys", "NotContainKeys", "ContainValue", "NotContainValue", "ContainValues", "NotContainValues");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        if (name.EndsWith("Keys", StringComparison.Ordinal))
        {
            return PatternResult.Manual($"{name}(...) checks several keys at once: use one ShouldContainKey/ShouldNotContainKey per key by hand.");
        }

        if (name.EndsWith("Value", StringComparison.Ordinal) || name.EndsWith("Values", StringComparison.Ordinal))
        {
            return PatternResult.Manual($"{name}(...) has no Shouldly equivalent (only ShouldContainKeyAndValue and ShouldNotContainValueForKey): check .Values by hand.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And/.WhoseValue or a value); Shouldly's ShouldContainKey returns nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out _))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (values.Count != 1)
        {
            return PatternResult.Manual($"{name}(...) with an unexpected argument list.");
        }

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                FluentAssertionsSyntax.NotNull(subject, model).WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("Should" + name)),
            assertion.ArgumentList));
    }
}
