using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: MassTransitStateMachine<T> / ISaga.
public class SagaPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["MassTransitStateMachine", "ISaga", "InitiatedBy", "Orchestrates"], StringComparer.Ordinal);

    public override string Id => "masstransit.saga";

    public override string Description => "MassTransitStateMachine<T> / ISaga → Manual (report only)";

    protected override string Reason => "MassTransit saga: the state-machine DSL (Initially/During/When/TransitionTo) has no structural mapping; a Wolverine Saga or Rebus Saga<TData> must be designed by hand.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindClasses(root, model, s_names);
}
