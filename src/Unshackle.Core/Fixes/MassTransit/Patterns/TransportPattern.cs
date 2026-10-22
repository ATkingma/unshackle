using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: UsingRabbitMq / UsingAzureServiceBus / UsingAmazonSqs / UsingInMemory transport.
public class TransportPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["UsingRabbitMq", "UsingAzureServiceBus", "UsingAmazonSqs", "UsingInMemory", "UsingActiveMq", "UsingGrpc", "UsingSqlServer", "UsingPostgres"], StringComparer.Ordinal);

    public override string Id => "masstransit.transport";

    public override string Description => "UsingRabbitMq / UsingAzureServiceBus / UsingAmazonSqs / UsingInMemory transport → Manual (report only)";

    protected override MassTransitCode Code => MassTransitCode.Transport;

    protected override string Reason => "MassTransit transport (Using...): topology, exchanges and queue names change meaning on another bus; Wolverine has UseRabbitMq/UseAzureServiceBus, Rebus Transport(t => t.Use...).";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindCalls(root, model, s_names);
}
