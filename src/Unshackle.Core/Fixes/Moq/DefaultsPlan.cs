using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq;

// The members whose Moq default (null) is pinned on the substitute, or why the mock cannot be converted.
public class DefaultsPlan
{
    public string? Blocker { get; init; }

    public IReadOnlyList<ISymbol> Pins { get; init; } = [];
}
