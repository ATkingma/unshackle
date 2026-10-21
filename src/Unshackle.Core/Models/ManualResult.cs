namespace Unshackle.Core.Models;

public record ManualResult(string Reason) : PatternResult
{
    // Set when the finding stays on the old library while the rest of the fix goes ahead (council round 3, Q2); None for an ordinary
    // Manual result. The reason then has no "Stays on ...: " prefix: the report renderer prints it (KeptReasonText.Rendered).
    public KeptReason Kept { get; init; }
}
