using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class NotBePattern : ShouldBePattern
{
    public override string Id => "fa.not-be";

    public override string Description => "x.Should().NotBe(y) → x.ShouldNotBe(y)";

    protected override string ShouldName => "ShouldNotBe";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "NotBe");
}
