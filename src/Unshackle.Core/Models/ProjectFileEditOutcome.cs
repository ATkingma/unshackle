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
}
