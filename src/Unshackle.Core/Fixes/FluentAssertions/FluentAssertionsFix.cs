using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions;

public class FluentAssertionsFix : IFix
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, MIT, no license check.
    public const string ShouldlyVersion = "4.3.0";

    // Without Shouldly a lambda-nested rewrite needs a second run (TAAK-116); the tests of single patterns need none.
    public FluentAssertionsFix()
    {
        AddedReferences = [];
    }

    // The Shouldly assembly comes from the composition root: Core does no I/O.
    public FluentAssertionsFix(IReadOnlyList<MetadataReference> shouldly)
    {
        ArgumentNullException.ThrowIfNull(shouldly);
        AddedReferences = shouldly;
    }

    public string Id => "fluentassertions";

    public IReadOnlyList<MetadataReference> AddedReferences { get; }

    public IReadOnlyList<string> RemovedNamespaces { get; } = ["FluentAssertions", "FluentAssertions.Execution"];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Shouldly"];

    public IReadOnlyList<string> RemovedExtensionMethods { get; } = ["Should"];

    public PackageMigration Package { get; } =
        new() { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = ShouldlyVersion }] };

    // The assertion patterns, also the links AndChainPattern rewrites after splitting a chain.
    private static readonly IPattern[] s_assertions =
    [
        new ShouldBePattern(),
        new NotBePattern(),
        new HaveCountPattern(),
        new BeTrueFalsePattern(),
        new BeNullPattern(),
        new BeEmptyPattern(),
        new ContainPattern(),
        new BeEquivalentToStringPattern(),
        new ThrowPattern(),
        new ThrowAsyncPattern(),
        new ThrowExactlyPattern(),
        new InvokingPattern(),
        new BeGreaterLessPattern(),
        new NotThrowPattern(),
        new ContainKeyPattern(),
        new BeEquivalentToObjectPattern(),
        new OnlyContainPattern(),
        new BeOfTypePattern(),
        new StringStartEndPattern(),
        new ContainSinglePattern(),
        new EqualSequencePattern(),
        new HaveLengthPattern(),
        new BeApproximatelyPattern(),
        new BeSameAsPattern(),
        new NotThrowAsyncPattern(),
        new MatchPattern(),
        new DirectMappingPattern(),
    ];

    public IReadOnlyList<IPattern> Patterns { get; } =
    [
        // First: a scope of only assertions becomes ShouldSatisfyAllConditions, whose lambdas the patterns below rewrite.
        new AssertionScopePattern(),
        // Then: its loop body keeps the FluentAssertions assertions, which the patterns below then rewrite as statements.
        new AllSatisfyPattern(),
        // Before the assertion patterns, which report a link after .And as Manual.
        new AndChainPattern(s_assertions),
        .. s_assertions,
        new CustomAssertionPattern(),
    ];
}
