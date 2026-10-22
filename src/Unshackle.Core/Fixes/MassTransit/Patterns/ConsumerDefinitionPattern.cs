using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: class D : ConsumerDefinition<T>.
public class ConsumerDefinitionPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["ConsumerDefinition"], StringComparer.Ordinal);

    public override string Id => "masstransit.consumer-definition";

    public override string Description => "class D : ConsumerDefinition<T> → Manual (report only)";

    protected override MassTransitCode Code => MassTransitCode.ConsumerDefinition;

    protected override string Reason => "MassTransit consumer definition: endpoint name, concurrency and per-endpoint middleware are configuration with no 1:1 equivalent on Wolverine or Rebus.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindClasses(root, model, s_names);
}
