using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The Received() checks a Verify()/VerifyAll expands into, with the warning that names the setups, or why it cannot expand.
public class VerifyAllPlan
{
    public MoqBlocker? Blocker { get; init; }

    public IReadOnlyList<StatementSyntax> Statements { get; init; } = [];

    public string? Warning { get; init; }

    public static VerifyAllPlan Blocked(MoqReason code, string reason) => new() { Blocker = MoqBlocker.Of(code, reason) };
}
