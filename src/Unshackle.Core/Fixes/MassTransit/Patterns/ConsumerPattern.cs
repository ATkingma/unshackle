using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: class C : IConsumer<T>.
public class ConsumerPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["IConsumer"], StringComparer.Ordinal);

    public override string Id => "masstransit.consumer";

    public override string Description => "class C : IConsumer<T> → Manual (report only)";

    protected override string Reason => "MassTransit consumer (IConsumer<T>.Consume(ConsumeContext<T>)): Rebus IHandleMessages<T>.Handle(T) with IMessageContext, Wolverine a Handle(T) convention method; context members (Headers, RespondAsync, Redeliver) need a by-hand mapping.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindClasses(root, model, s_names);
}
