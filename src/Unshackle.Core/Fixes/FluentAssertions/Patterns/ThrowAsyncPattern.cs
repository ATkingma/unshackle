using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ThrowAsyncPattern : IPattern
{
    public string Id => "fa.throw-async";

    public string Description => "await act.Should().ThrowAsync<T>()[.WithMessage(p)/.Where(..)/.WithInnerException<TI>()] → await Should.ThrowAsync<T>(act)[.Message.Should...]";

    // As ThrowPattern; the chain must be awaited, and a `.ConfigureAwait(...)` before the await is kept.
    // ThrowExactlyAsync and Awaiting subjects belong to ThrowExactlyPattern and InvokingPattern, which call the same rewrite.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        ThrowSyntax.FindThrowAsync(root, model, t => !ThrowSyntax.IsExactly(t) && !ThrowSyntax.IsInvoking(t, model));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => ThrowChains.Rewrite(node, model);
}
