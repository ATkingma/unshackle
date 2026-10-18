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

    public IReadOnlyList<string> RemovedNamespaces { get; } = [
        "FluentAssertions", "FluentAssertions.Execution", "FluentAssertions.Extensions", "FluentAssertions.Primitives",
        // Sub-namespaces: an unused one goes with the package, a used one keeps it (TAAK-283).
        "FluentAssertions.Collections", "FluentAssertions.Common", "FluentAssertions.Data", "FluentAssertions.Equivalency",
        "FluentAssertions.Events", "FluentAssertions.Formatting", "FluentAssertions.Numeric", "FluentAssertions.Specialized",
        "FluentAssertions.Streams", "FluentAssertions.Types", "FluentAssertions.Xml",
    ];

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
        new EquivalentOptionsPattern(),
        new ContainEquivalentOfPattern(),
        new OnlyContainPattern(),
        new BeOfTypePattern(),
        new StringStartEndPattern(),
        new ContainSinglePattern(),
        new EqualSequencePattern(),
        new NotEqualPattern(),
        new OrderPattern(),
        new NotContainNullsPattern(),
        new HaveLengthPattern(),
        new BeApproximatelyPattern(),
        new BeSameAsPattern(),
        new NotThrowAsyncPattern(),
        new MatchPattern(),
        new DirectMappingPattern(),
    ];

    public IReadOnlyList<IPattern> Patterns { get; } =
    [
        // Very first, on the unchanged baseline the class plans are computed from: own assertion classes and their calls,
        // whose .And chains it splits so the FluentAssertions links reach AndChainPattern and the assertion patterns.
        new CustomAssertionPattern(),
        // Then: a scope of only assertions becomes ShouldSatisfyAllConditions, whose lambdas the patterns below rewrite.
        new AssertionScopePattern(),
        // Then: its loop body keeps the FluentAssertions assertions, which the patterns below then rewrite as statements.
        new AllSatisfyPattern(),
        // `_ = x.Should()...;` becomes a plain statement, which the patterns below treat as a discarded result.
        new DiscardAssignmentPattern(),
        // `var act = x.Invoking(...)` becomes a delegate local, so the Throw patterns below see a delegate subject.
        new InvokingLocalPattern(),
        // Before the assertion patterns, which report a link after .And as Manual.
        new AndChainPattern(s_assertions),
        .. s_assertions,
        // Catch sites of the assertion exception, e.g. a retry helper around assertions (TAAK-140).
        new CatchAssertionExceptionPattern(),
        // No assertion: 5.Seconds() and 1.March(2020) from FluentAssertions.Extensions (TAAK-224).
        new TimeSpanExtensionsPattern(),
    ];
}
