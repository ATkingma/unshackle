using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeEquivalentToStringPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-equivalent-to-string";

    public override string Description => "s.Should().BeEquivalentTo(\"x\") → s.ShouldBe(\"x\", StringCompareShould.IgnoreCase)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEquivalentTo", "NotBeEquivalentTo")
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingType.Name: "StringAssertions" });

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        if (context.Name == "NotBeEquivalentTo")
        {
            return PatternResult.Manual("NotBeEquivalentTo(...) has no Shouldly equivalent: ShouldNotBe has no case-insensitive string overload.");
        }

        if (context.Values.Count != 1)
        {
            return PatternResult.Manual($"{context.Name}(...) with an unexpected argument list.");
        }

        // Both ignore only the casing (ordinal): whitespace, line endings and culture-specific folding still differ.
        // The overload is ShouldBe(expected, customMessage, options), so `because` sits before the compare option.
        ArgumentSyntax ignoreCase = SyntaxFactory.Argument(SyntaxFactory.ParseExpression("StringCompareShould.IgnoreCase"));
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), "ShouldBe", context.Values[0], context.Because, ignoreCase));
    }
}
