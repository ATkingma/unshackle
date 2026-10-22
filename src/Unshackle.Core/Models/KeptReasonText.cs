namespace Unshackle.Core.Models;

/// <summary>
/// The one rendering of a kept finding's reason (council round 6, Q1 and Q2): the report, the console and a reason that quotes a kept
/// reason all print it through <see cref="Rendered"/>, with the prefix of the fix the code stays on.
/// </summary>
public static class KeptReasonText
{
    /// <summary>The prefix of a finding that stays on AutoMapper while the rest is migrated.</summary>
    public const string AutoMapperPrefix = "Stays on AutoMapper: ";

    /// <summary>The prefix of a finding that stays on MediatR next to Mediator in a partial migration.</summary>
    public const string MediatRPrefix = "Stays on MediatR next to Mediator: ";

    /// <summary>The prefix printed in front of the reason of a kept finding with this code; empty for <see cref="KeptReason.None"/>.</summary>
    public static string Prefix(KeptReason code) => code switch
    {
        KeptReason.None => string.Empty,
        KeptReason.MediatRNextToMediator => MediatRPrefix,
        _ => AutoMapperPrefix,
    };

    /// <summary>The reason of a Manual result as the report renders it: with the prefix of its fix for a kept finding.</summary>
    public static string Rendered(ManualResult manual)
    {
        ArgumentNullException.ThrowIfNull(manual);
        // The reason code (council round 10, C1) goes after the kept prefix, as the fixes wrote it in the text before.
        return Prefix(manual.Kept) + (manual.Code is null ? string.Empty : manual.Code + ": ") + manual.Reason;
    }
}
