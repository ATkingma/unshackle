using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// TAAK-311 (council A1): mock.Setup(m =&gt; m.Map&lt;B&gt;(a)).Returns(b); (Moq) or mock.Map&lt;B&gt;(a).Returns(b); (NSubstitute) → removed, when
/// <see cref="MapOwners"/> lets the mock go: every stub of it provably returns what the generated method returns (<see cref="MockedMappers"/>)
/// and every map it serves is migrated, so the class under test calls the generated method instead. Each removed stub warns, with file:line.
/// Any other stub stays (Manual through <see cref="MapCallPattern"/>, naming the test).
/// </summary>
public class MockSetUpPattern : ISolutionPattern, ISolutionFinder
{
    public string Id => "automapper.mock-set-up";

    public string Description =>
        "Stub of IMapper.Map in a test whose value provably equals the generated mapping → removed (the class under test calls the generated method)";

    // Without the solution the plan is unknown: nothing is removed.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => [];

    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken) =>
        [.. (await StubsAsync(root, model, solution, cancellationToken)).Select(s => s.Stub.Statement)];

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("Removing a stub of IMapper needs the whole solution (the class under test and its maps); run the fix on the solution.");

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        SyntaxNode root = node.SyntaxTree.GetRoot(cancellationToken);
        if ((await StubsAsync(root, model, solution, cancellationToken)).FirstOrDefault(s => s.Stub.Statement == node) is not { Stub: { } stub, Mock: { } mock })
        {
            return PatternResult.Manual("This stub of IMapper is no longer one whose mock the plan lets go.");
        }

        string classes = string.Join(", ", mock.Classes.Select(c => c.Name));
        return new FixedResult([], [])
        {
            Warning = $"The stub of IMapper.Map<{stub.To.Name}> at {stub.Where} is removed: {classes} now calls the generated mapping method, which returns the "
                + $"stubbed values for the {stub.From?.Name} the test creates. The test only passes as before if the class under test maps that same object unchanged.",
        };
    }

    private static async Task<List<(MapStub Stub, MockedMapper Mock)>> StubsAsync(
        SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        if (!root.ToString().Contains("IMapper", StringComparison.Ordinal))
        {
            return [];
        }

        List<MockedMapper> mocks = MockedMappers.Find(root, model);
        if (mocks.Count == 0)
        {
            return [];
        }

        MapOwners owners = await MapOwners.ForAsync(solution, model, cancellationToken);
        return [.. mocks.Where(m => owners.IsDischarged(m.Key)).SelectMany(m => m.Stubs.Select(s => (s, m)))];
    }
}
