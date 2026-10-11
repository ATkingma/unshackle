using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeNullPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-null";

    public override string Description => "x.Should().BeNull() → x.ShouldBeNull(), NotBeNull() → ShouldNotBeNull()";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeNull", "NotBeNull");

    // The same extension works for reference types and Nullable<T>, so no type switch is needed.
    protected override PatternResult RewriteAssertion(AssertionContext context) =>
        PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            context.Subject.WithoutTrailingTrivia(), context.Name == "BeNull" ? "ShouldBeNull" : "ShouldNotBeNull", context.Because));
}
