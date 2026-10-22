namespace Unshackle.Core.Models;

// A finding that needs no change and blocks nothing: a note for the report (council round 8, S2), e.g. one row of a report-only
// inventory. Neither Fixed nor Manual: it does not count as fixed and does not undo an all-or-nothing fix.
public record InfoResult(string Text) : PatternResult
{
    // Note for an ordinary Info row; Header for the "inventory only" row of a report-only fix, which every count leaves out (council round 10, G1).
    public InfoKind Kind { get; init; }
}
