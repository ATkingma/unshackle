namespace Unshackle.Core.Models;

public class FixRequest
{
    public required string FixId { get; init; }

    // Files one at a time instead of in parallel (TAAK-298); the output is the same either way.
    public bool Sequential { get; init; }

    // How many files at once when the fix allows it (TAAK-298, council round 7); null is min(ProcessorCount, 8).
    public int? MaxDegree { get; init; }

    // Run the patterns on every file, also on one the pre-filter skips (TAAK-298); for comparing runs, the output is the same.
    public bool CheckEveryFile { get; init; }
}
