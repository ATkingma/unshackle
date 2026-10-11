using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;

namespace Unshackle.Core.Fixes.FluentAssertions;

public class FluentAssertionsFix : IFix
{
    public string Id => "fluentassertions";

    public IReadOnlyList<string> RemovedNamespaces { get; } = ["FluentAssertions"];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Shouldly"];

    public IReadOnlyList<IPattern> Patterns { get; } =
    [
        // First: its loop body keeps the FluentAssertions assertions, which the patterns below then rewrite as statements.
        new AllSatisfyPattern(),
        new ShouldBePattern(),
        new HaveCountPattern(),
        new BeTrueFalsePattern(),
        new BeNullPattern(),
        new BeEmptyPattern(),
        new ContainPattern(),
        new BeEquivalentToStringPattern(),
        new ThrowPattern(),
        new ThrowAsyncPattern(),
        new BeGreaterLessPattern(),
        new NotThrowPattern(),
        new ContainKeyPattern(),
        new BeEquivalentToObjectPattern(),
        new OnlyContainPattern(),
    ];
}
