using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions;

public class FluentAssertionsFix : IFix
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, MIT, no license check.
    public const string ShouldlyVersion = "4.3.0";

    public string Id => "fluentassertions";

    public IReadOnlyList<string> RemovedNamespaces { get; } = ["FluentAssertions"];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Shouldly"];

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
    ];

    public IReadOnlyList<IPattern> Patterns { get; } =
    [
        // First: its loop body keeps the FluentAssertions assertions, which the patterns below then rewrite as statements.
        new AllSatisfyPattern(),
        // Before the assertion patterns, which report a link after .And as Manual.
        new AndChainPattern(s_assertions),
        .. s_assertions,
        new AssertionScopePattern(),
        new CustomAssertionPattern(),
    ];
}
