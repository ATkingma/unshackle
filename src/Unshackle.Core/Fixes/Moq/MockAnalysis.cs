using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq;

// The answer of MockUsage for one mock: why it stays Moq, or the defaults to pin when it becomes a substitute.
public class MockAnalysis
{
    public MoqBlocker? Blocker { get; init; }

    public IReadOnlyList<ISymbol> Pins { get; init; } = [];

    // No use at all besides its creation (council round 9, Q4: Fixed with an Info line).
    public bool Unused { get; init; }

    public ITypeSymbol? Mocked { get; init; }

    public static MockAnalysis Blocked(MoqBlocker blocker) => new() { Blocker = blocker };

    public static MockAnalysis Blocked(MoqReason code, string reason) => Blocked(MoqBlocker.Of(code, reason));
}
