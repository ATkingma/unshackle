using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: AddMassTransitTestHarness / GetTestHarness.
public class TestHarnessPattern : DetectorPattern
{
    private static readonly HashSet<string> s_names = new(["AddMassTransitTestHarness", "AddMassTransitInMemoryTestHarness", "GetTestHarness"], StringComparer.Ordinal);

    public override string Id => "masstransit.test-harness";

    public override string Description => "AddMassTransitTestHarness / GetTestHarness → Manual (report only)";

    protected override MassTransitCode Code => MassTransitCode.TestHarness;

    protected override string Reason => "MassTransit test harness: tests that wait on consumed or published messages need a new harness; Wolverine has TrackActivity, Rebus FakeBus and SagaFixture.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindCalls(root, model, s_names);
}
