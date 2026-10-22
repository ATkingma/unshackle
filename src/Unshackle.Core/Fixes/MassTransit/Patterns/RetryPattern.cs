using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: UseMessageRetry / UseRetry / UseDelayedRedelivery.
public class RetryPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["UseMessageRetry", "UseRetry", "UseDelayedRedelivery", "UseScheduledRedelivery", "UseKillSwitch", "UseCircuitBreaker"], StringComparer.Ordinal);

    public override string Id => "masstransit.retry";

    public override string Description => "UseMessageRetry / UseRetry / UseDelayedRedelivery → Manual (report only)";

    protected override MassTransitCode Code => MassTransitCode.RetryPolicy;

    protected override string Reason => "MassTransit retry or redelivery policy: attempt counts and delays must be re-expressed; Wolverine uses error-handling policies, Rebus RetryStrategy.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindCalls(root, model, s_names);
}
