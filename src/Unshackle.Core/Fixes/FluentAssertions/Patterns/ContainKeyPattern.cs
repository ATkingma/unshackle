using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainKeyPattern : FluentAssertionPattern
{
    public override string Id => "fa.contain-key";

    public override string Description => "dict.Should().ContainKey(k) → dict.ShouldContainKey(k), NotContainKey → ShouldNotContainKey";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(
            root, model, "ContainKey", "NotContainKey", "ContainKeys", "NotContainKeys", "ContainValue", "NotContainValue", "ContainValues", "NotContainValues");

    // Before the argument split: ContainKeys/ContainValues take params, which it would report as becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        string name = context.Name;
        if (name.EndsWith("Keys", StringComparison.Ordinal))
        {
            return PatternResult.Manual($"{name}(...) checks several keys at once: use one ShouldContainKey/ShouldNotContainKey per key by hand.");
        }

        if (name.EndsWith("Value", StringComparison.Ordinal) || name.EndsWith("Values", StringComparison.Ordinal))
        {
            return PatternResult.Manual($"{name}(...) has no Shouldly equivalent (only ShouldContainKeyAndValue and ShouldNotContainValueForKey): check .Values by hand.");
        }

        return null;
    }

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        if (context.Values.Count != 1)
        {
            return PatternResult.Manual($"{context.Name}(...) with an unexpected argument list.");
        }

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                FluentAssertionsSyntax.NotNull(context.Subject, context.Model).WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("Should" + context.Name)),
            FluentAssertionsSyntax.Arguments([.. context.Values, context.Because])));
    }
}
