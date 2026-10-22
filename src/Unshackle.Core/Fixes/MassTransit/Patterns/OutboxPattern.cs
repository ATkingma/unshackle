using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: AddEntityFrameworkOutbox / UseBusOutbox / in-memory outbox.
public class OutboxPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["AddEntityFrameworkOutbox", "AddMongoDbOutbox", "UseBusOutbox", "UseEntityFrameworkOutbox", "UseMongoDbOutbox", "AddInMemoryInboxOutbox", "UseInMemoryOutbox"], StringComparer.Ordinal);

    public override string Id => "masstransit.outbox";

    public override string Description => "AddEntityFrameworkOutbox / UseBusOutbox / in-memory outbox → Manual (report only)";

    protected override MassTransitCode Code => MassTransitCode.Outbox;

    protected override string Reason => "MassTransit outbox: delivery guarantees and its database tables are semantics; Wolverine has a durable inbox/outbox, Rebus an outbox in Rebus.SqlServer.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindCalls(root, model, s_names);
}
