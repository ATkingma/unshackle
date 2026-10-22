using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>CircuitBreaker(Async)</c> and <c>AdvancedCircuitBreaker(Async)</c> (queue row 8): always Manual. The probe (TAAK-308) showed v7's
/// consecutive-failure count and v8's failure-ratio window disagree: on the sequence F S F F F with a threshold of 3, v7 breaks
/// on the third consecutive failure and v8 (FailureRatio 1.0, MinimumThroughput 3) does not, because the success stays in its window.
/// v8 also rejects a BreakDuration below 500 ms and a MinimumThroughput below 2, which v7 allows.
/// </summary>
public class CircuitBreakerPattern : IPattern
{
    public string Id => "polly.circuit-breaker";

    public string Description => "CircuitBreaker(Async) / AdvancedCircuitBreaker(Async): Manual, v7 and v8 break on different failure sequences";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CircuitBreaker" or "CircuitBreakerAsync" or "AdvancedCircuitBreaker" or "AdvancedCircuitBreakerAsync" }
                && model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingAssembly.Name: PollyRetry.PollyAssembly });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual(PollyReason.CircuitBreakerNotEquivalent,
            "v7 counts consecutive failures (a success resets the count) while v8 breaks on a failure ratio over a sampling window; with the same threshold they break on different call sequences, and v8 rejects BreakDuration < 500 ms and MinimumThroughput < 2. Choose the v8 ratio and window by hand.");
}
