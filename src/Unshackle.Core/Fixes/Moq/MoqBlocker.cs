using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq;

// Why a mock, call or argument cannot be rewritten: the code and the text of its Manual finding.
public class MoqBlocker
{
    public required MoqReason Code { get; init; }

    public required string Reason { get; init; }

    public static MoqBlocker Of(MoqReason code, string reason) => new() { Code = code, Reason = reason };

    public PatternResult ToResult() => PatternResult.Manual(Code, Reason);
}
