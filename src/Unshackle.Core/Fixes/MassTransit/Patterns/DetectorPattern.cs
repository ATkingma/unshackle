using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// Template Method for the report-only detector: each subclass finds one MassTransit feature; every node is a Manual row with the
// feature's reason plus the shared target guidance (council round 8, T1-T4: no rewrite, no TODO skeleton).
public abstract class DetectorPattern : IPattern
{
    public abstract string Id { get; }

    public abstract string Description { get; }

    // What the feature is and what moving it means; the shared guidance follows.
    protected abstract string Reason { get; }

    public abstract IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => PatternResult.Manual(Reason + MassTransitSyntax.Guidance);
}
