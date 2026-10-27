namespace Unshackle.Core.Models;

public record ManualResult(string Reason) : PatternResult
{
    // Set when the finding stays on the old library while the rest of the fix goes ahead (council round 3, Q2); None for an ordinary
    // Manual result. The reason then has no "Stays on ...: " prefix: the report renderer prints it (KeptReasonText.Rendered).
    public KeptReason Kept { get; init; }

    // The typed reason code (council round 10, C1), for example "PolicyNotLocal"; null for a Manual result without one. Separate from
    // Kept: a code names why, Kept says whether the rest goes ahead. The reason then has no "Code: " prefix: the renderer prints it.
    public string? Code { get; init; }

    // The full list of dependents a kept-for-dependent project row names (council round 14, M11): the row text caps it, the JSON report
    // carries all of them. Null for every other Manual result.
    public IReadOnlyList<string>? Dependents { get; init; }
}
