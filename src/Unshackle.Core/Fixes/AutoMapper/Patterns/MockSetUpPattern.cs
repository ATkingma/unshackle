using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// TAAK-311 (council A1): mock.Setup(m =&gt; m.Map&lt;B&gt;(a)).Returns(b); (Moq) or mock.Map&lt;B&gt;(a).Returns(b); (NSubstitute) → removed, when
/// <see cref="MapOwners"/> lets the mock go: every stub of it provably returns what the generated method returns (<see cref="MockedMappers"/>)
/// and every map it serves is migrated, so the class under test calls the generated method instead. Each removed stub warns, with file:line.
/// Any other stub stays (Manual through <see cref="MapCallPattern"/>, naming the test). Council round 11: a stub with an argument matcher
/// goes under A7 (<see cref="MockedMappers.Disproof"/>), a check of a Map call keeps the mock (A6, <see cref="MockSetUpCode.VerifyOnMapper"/>),
/// and a mock without stubs whose classes never read what Map returns lets them migrate with an Info line on the mock (A8).
/// </summary>
public class MockSetUpPattern : ISolutionPattern, ISolutionFinder
{
    private readonly IProjectProperties? _projectProperties;

    // Without project properties (tests, a host without MSBuild) packability is guessed from the compilation (TypeArgumentClosure).
    public MockSetUpPattern()
        : this(null)
    {
    }

    // TAAK-324: IsPackable as MSBuild sees it decides whether a public generic member closes its type arguments (council round 10, R1).
    public MockSetUpPattern(IProjectProperties? projectProperties)
    {
        _projectProperties = projectProperties;
    }

    public string Id => "automapper.mock-set-up";

    public string Description =>
        "Stub of IMapper.Map in a test whose value provably equals the generated mapping → removed (the class under test calls the generated method)";

    // Without the solution the plan is unknown: nothing is removed.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => [];

    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        (List<(MapStub Stub, MockedMapper Mock)> stubs, List<MockedMapper> bare) = await DischargedAsync(root, model, solution, _projectProperties, cancellationToken);
        return [.. stubs.Select(s => (SyntaxNode)s.Stub.Statement), .. bare.Select(m => m.Declaration)];
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("Removing a stub of IMapper needs the whole solution (the class under test and its maps); run the fix on the solution.");

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        SyntaxNode root = node.SyntaxTree.GetRoot(cancellationToken);
        (List<(MapStub Stub, MockedMapper Mock)> stubs, List<MockedMapper> bare) = await DischargedAsync(root, model, solution, _projectProperties, cancellationToken);
        if (bare.FirstOrDefault(m => m.Declaration == node) is { } unstubbed)
        {
            return PatternResult.Note($"The mock of IMapper at {unstubbed.Where} had no stubs; Map now returns a real object: "
                + $"{string.Join(", ", unstubbed.Classes.Select(c => c.Name))} calls the generated mapping method, and never reads what it returns.");
        }

        if (stubs.FirstOrDefault(s => s.Stub.Statement == node) is not { Stub: { } stub, Mock: { } mock })
        {
            return PatternResult.Manual("This stub of IMapper is no longer one whose mock the plan lets go.");
        }

        string classes = string.Join(", ", mock.Classes.Select(c => c.Name));
        string matched = MockedMappers.IsMatcher(stub.Source, model) ? $" (the stub matched any {stub.From?.Name}; the test builds one)" : "";
        return new FixedResult([], [])
        {
            Warning = $"The stub of IMapper.Map<{stub.To.Name}> at {stub.Where} is removed: {classes} now calls the generated mapping method, which returns the "
                + $"stubbed values for the {stub.From?.Name} the test creates{matched}. The test only passes as before if the class under test maps that same object unchanged.",
        };
    }

    // The stubs that go (with their mock) and the mocks without stubs whose classes migrate (council A8), of this file.
    private static async Task<(List<(MapStub Stub, MockedMapper Mock)> Stubs, List<MockedMapper> Bare)> DischargedAsync(
        SyntaxNode root, SemanticModel model, Solution solution, IProjectProperties? projectProperties, CancellationToken cancellationToken)
    {
        if (!root.ToString().Contains("IMapper", StringComparison.Ordinal))
        {
            return ([], []);
        }

        List<MockedMapper> mocks = MockedMappers.Find(root, model);
        if (mocks.Count == 0)
        {
            return ([], []);
        }

        MapOwners owners = await MapOwners.ForAsync(solution, model, projectProperties, cancellationToken);
        List<MockedMapper> discharged = [.. mocks.Where(m => owners.IsDischarged(m.Key))];
        return ([.. discharged.SelectMany(m => m.Stubs.Select(s => (s, m)))], [.. discharged.Where(m => m.Stubs.Count == 0)]);
    }
}
