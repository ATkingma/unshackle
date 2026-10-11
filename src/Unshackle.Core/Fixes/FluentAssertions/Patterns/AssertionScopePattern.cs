using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class AssertionScopePattern : IPattern
{
    public string Id => "fa.assertion-scope";

    public string Description => "new AssertionScope() → Manual: group the assertions with ShouldSatisfyAllConditions";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<BaseObjectCreationExpressionSyntax>()
            .Where(c => FluentAssertionsSyntax.IsAssertionScope(model.GetTypeInfo(c).Type));

    // Never rewritten: scopes also hold non-assertion statements, nested scopes and Discard()/AddReportable.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("AssertionScope collects all failures; Shouldly has no scope. Rewrite the block to x.ShouldSatisfyAllConditions(() => ..., () => ...) and remove the scope.");
}
