using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Polly;

// The outcome of analysing one Polly v7 construct: the replacement node, or why it stays Manual.
public class PollyPlan
{
    public SyntaxNode? Node { get; init; }

    public string? Reason { get; init; }

    public static PollyPlan Of(SyntaxNode node) => new() { Node = node };

    public static PollyPlan Manual(PollyReason code, string text) => new() { Reason = $"{code}: {text}" };
}
