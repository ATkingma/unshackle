using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// Polly v7 constructs with no proven v8 mapping (queue row 12): reported Manual with a reason code so the project keeps its v7 package.
/// Fallback, Bulkhead, Cache, RateLimit, PolicyRegistry, Context, ExecuteAndCapture and the Polly.Contrib.WaitAndRetry Backoff helpers.
/// </summary>
public class UnsupportedPattern : IPattern
{
    private const string ContribAssembly = "Polly.Contrib.WaitAndRetry";

    public string Id => "polly.unsupported";

    public string Description => "Fallback, Bulkhead, Cache, RateLimit, PolicyRegistry, Context, ExecuteAndCapture, Contrib Backoff: Manual with a reason code";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => Classify(n, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Classify(node, model) is (PollyReason code, string text)
            ? PatternResult.Manual(code, text)
            : PatternResult.Manual(PollyReason.PolicyNotLocal, "unexpected node.");

    private static (PollyReason, string)? Classify(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: string name } } call
                when model.GetSymbolInfo(call).Symbol is IMethodSymbol method:
                if (method.ContainingAssembly.Name == ContribAssembly)
                {
                    return (PollyReason.ContribBackoffNotMapped, "Polly.Contrib.WaitAndRetry delay sequences have no v8 counterpart with proven equal values (v8 Exponential/DecorrelatedJitter backoff is a different generator).");
                }

                if (method.ContainingAssembly.Name != PollyRetry.PollyAssembly)
                {
                    return null;
                }

                return name switch
                {
                    "Fallback" or "FallbackAsync" => (PollyReason.FallbackNotMapped, "v7 Fallback policies wrap fallback delegates and onFallback callbacks; v8 AddFallback has a different filter/action shape that is not proven equal."),
                    "Bulkhead" or "BulkheadAsync" => (PollyReason.BulkheadNotMapped, "v8 has no bulkhead strategy; use a ConcurrencyLimiter rate limiter by hand (queue and rejection semantics differ)."),
                    "Cache" or "CacheAsync" => (PollyReason.CacheNotMapped, "v8 has no cache strategy; Polly.Caching is not ported."),
                    "RateLimit" or "RateLimitAsync" => (PollyReason.RateLimitNotMapped, "v7 RateLimit is a token bucket with its own retry-after contract; v8 AddRateLimiter uses System.Threading.RateLimiting with different semantics."),
                    "ExecuteAndCapture" or "ExecuteAndCaptureAsync" => (PollyReason.ExecuteAndCaptureNotMapped, "v8 has no ExecuteAndCapture; ExecuteOutcomeAsync returns an Outcome<T> with a different shape."),
                    _ => null,
                };
            case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax when model.GetTypeInfo(node).Type is INamedTypeSymbol { ContainingAssembly.Name: PollyRetry.PollyAssembly } type:
                return type.Name switch
                {
                    "PolicyRegistry" => (PollyReason.PolicyRegistryNotMapped, "a PolicyRegistry holds policies by key and is read elsewhere; v8 uses ResiliencePipelineProvider, which needs a solution-wide rewrite."),
                    "Context" => (PollyReason.ContextNotMapped, "the v7 Context dictionary has no 1:1 v8 counterpart (ResilienceContext is pooled and keyed differently)."),
                    _ => null,
                };
            default:
                return null;
        }
    }
}
