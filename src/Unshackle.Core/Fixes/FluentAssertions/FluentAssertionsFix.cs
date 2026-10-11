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
        new ShouldBePattern(),
        new HaveCountPattern(),
        new BeTrueFalsePattern(),
        new BeNullPattern(),
        new BeEmptyPattern(),
        new ContainPattern(),
    ];
}
