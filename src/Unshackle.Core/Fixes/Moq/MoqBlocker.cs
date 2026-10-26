using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq;

// Why a mock, call or argument cannot be rewritten: the code and the text of its Manual finding.
public class MoqBlocker
{
    public required MoqReason Code { get; init; }

    public required string Reason { get; init; }

    public static MoqBlocker Of(MoqReason code, string reason) => new() { Code = code, Reason = reason };

    // Council round 13, M9: Configure() is never generated (ordering semantics unproven), only named as a manual route.
    public const string ConfigureRoute = "; manual route: NSubstitute's Configure() before the later setup, after checking which setup Moq would match (not generated, council round 13, M9)";

    // Council round 13, M8: a mock-use blocker reports under moq.unsupported (row 13), whichever pattern met it.
    public PatternResult ToResult() => PatternResult.Manual(Code, Code == MoqReason.SetupsOverlap ? Reason + ConfigureRoute : Reason) with { PatternId = RowId(Code) };

    private static string? RowId(MoqReason code) => code switch
    {
        MoqReason.StrictBehaviour or MoqReason.ProtectedMember or MoqReason.MockAs or MoqReason.MockInvocations or MoqReason.MockReset or MoqReason.MockRaise => "moq.unsupported",
        _ => null,
    };
}
