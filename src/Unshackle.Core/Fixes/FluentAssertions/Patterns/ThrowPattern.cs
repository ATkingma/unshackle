using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ThrowPattern : IPattern
{
    public string Id => "fa.throw";

    public string Description => "act.Should().Throw<T>()[.WithMessage(p)/.Where(..)/.WithInnerException<TI>()][.And/.Which] → Should.Throw<T>(act)[.Message.Should...]";

    // One node per Throw chain (see ThrowChains): the statement it starts, else its outermost node, plus the uses of a local it declares.
    // ThrowExactly and Invoking subjects belong to ThrowExactlyPattern and InvokingPattern, which call the same rewrite.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        ThrowSyntax.FindThrow(root, model, t => !ThrowSyntax.IsExactly(t) && !ThrowSyntax.IsInvoking(t, model));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => ThrowChains.Rewrite(node, model);
}
