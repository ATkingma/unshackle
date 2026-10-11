using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// The Throw/ThrowAsync/NotThrow assertions whose subject is x.Invoking(s => ...) or x.Awaiting(s => ...); the other
// patterns leave those to this one and turn the subject into `() => ...` with s replaced by x.
public class InvokingPattern : IPattern
{
    public string Id => "fa.invoking";

    public string Description => "x.Invoking(s => s.M()).Should().Throw<T>() / Awaiting / NotThrow → Should.Throw<T>(() => x.M()) etc.";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        ThrowSyntax.FindThrow(root, model, t => ThrowSyntax.IsInvoking(t, model))
            .Concat(ThrowSyntax.FindThrowAsync(root, model, t => ThrowSyntax.IsInvoking(t, model)))
            .Concat(ThrowSyntax.FindNotThrow(root, model, t => ThrowSyntax.IsInvoking(t, model)));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => ThrowSyntax.Rewrite(node, model);
}
