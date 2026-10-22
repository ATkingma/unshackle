using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: AddMassTransit(...) registration.
public class RegistrationPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["AddMassTransit"], StringComparer.Ordinal);

    public override string Id => "masstransit.registration";

    public override string Description => "AddMassTransit(...) registration → Manual (report only)";

    protected override string Reason => "MassTransit bus registration (AddMassTransit): endpoint naming, consumer discovery and bus start-up are configuration; Wolverine uses UseWolverine on the host, Rebus AddRebus with routing.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindCalls(root, model, s_names);
}
