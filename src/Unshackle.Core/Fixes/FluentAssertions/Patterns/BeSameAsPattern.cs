using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Both libraries compare with ReferenceEquals (probed: same, different, both null, one null, boxed values).
public class BeSameAsPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-same-as";

    public override string Description => "x.Should().BeSameAs(y) → x.ShouldBeSameAs(y), NotBeSameAs → ShouldNotBeSameAs";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeSameAs", "NotBeSameAs");

    protected override PatternResult RewriteAssertion(AssertionContext context) =>
        context.Values.Count != 1
            ? PatternResult.Manual($"{context.Name}(...) with an unexpected argument list.")
            : PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
                context.Subject.WithoutTrivia(), "Should" + context.Name, context.Values[0], context.Because));
}
