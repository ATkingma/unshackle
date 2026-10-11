using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeEmptyPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-empty";

    public override string Description => "x.Should().BeEmpty() → x.ShouldBeEmpty(), NotBeEmpty() → ShouldNotBeEmpty()";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEmpty", "NotBeEmpty");

    // Shouldly has ShouldBeEmpty for string and IEnumerable<T> (also dictionaries), so no type switch is needed.
    protected override PatternResult RewriteAssertion(AssertionContext context) =>
        PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            context.Subject.WithoutTrailingTrivia(), context.Name == "BeEmpty" ? "ShouldBeEmpty" : "ShouldNotBeEmpty", context.Because));
}
