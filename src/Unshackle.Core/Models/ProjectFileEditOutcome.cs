namespace Unshackle.Core.Models;

// What one ProjectFileEdit did in one project: the file and line it touched and its changes, or why it stays manual.
public class ProjectFileEditOutcome
{
    public required ProjectFileEdit Edit { get; init; }

    public required string FilePath { get; init; }

    // 1-based line in the old text (an added element: the line after its neighbour); 0 for the whole file.
    public required int Line { get; init; }

    // The new text of each touched file; empty when the edit stays manual.
    public IReadOnlyList<ProjectFileChange> Changes { get; init; } = [];

    // Why the edit was not made; null when it was.
    public string? ManualReason { get; init; }

    // The reason code of ManualReason (council round 10, C1), for example a ProjectBlockingReason; null without one.
    public string? ManualCode { get; init; }

    // Something the report adds to an applied edit (council round 10, G2), for example why an old PackageVersion stays; null without one.
    public string? Note { get; init; }

    // Why the edit was deliberately not made (council round 10, P8), reported as an Info row; null when it was made or is Manual.
    public string? KeptInfo { get; init; }

    // Old <PackageVersion> elements the edit left for the solution-level pass (council round 12, G7).
    public IReadOnlyList<OldPackageVersion> PendingOldVersions { get; init; } = [];
}
