using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeEquivalentToStringPattern : IPattern
{
    public string Id => "fa.be-equivalent-to-string";

    public string Description => "s.Should().BeEquivalentTo(\"x\") → s.ShouldBe(\"x\", StringCompareShould.IgnoreCase)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEquivalentTo", "NotBeEquivalentTo")
            .Where(i => i.Expression is MemberAccessExpressionSyntax
                && model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingType.Name: "StringAssertions" });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        if (name == "NotBeEquivalentTo")
        {
            return PatternResult.Manual("NotBeEquivalentTo(...) has no Shouldly equivalent: ShouldNotBe has no case-insensitive string overload.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And/.Which or a value); Shouldly's ShouldBe returns nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because)
            || values.Count != 1)
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        // Both ignore only the casing (ordinal): whitespace, line endings and culture-specific folding still differ.
        // The overload is ShouldBe(expected, customMessage, options), so `because` sits before the compare option.
        ArgumentSyntax ignoreCase = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(" StringCompareShould.IgnoreCase"));
        ArgumentSyntax[] arguments = because is null
            ? [values[0], ignoreCase]
            : [values[0], because.WithLeadingTrivia(SyntaxFactory.Space), ignoreCase];
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("ShouldBe")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))));
    }
}
