namespace Unshackle.Core.Models;

// What the project-file editor needs to know about the solution (council round 10, G2/G3): a shared Directory.*.props is edited, and an old
// central PackageVersion removed, only when every project under it is loaded and none of them is blocked.
public class ProjectFileEditContext
{
    // Nothing known: no project counts as loaded, so a shared file is never edited.
    public static ProjectFileEditContext None { get; } = new()
    {
        LoadedProjects = new HashSet<string>(),
        BlockedProjects = new Dictionary<string, string>(),
    };

    // Full paths of the project files the solution loaded (compared with the editor's path comparer).
    public required IReadOnlySet<string> LoadedProjects { get; init; }

    // Full path of a loaded project file -> why its own file edits are blocked (council round 10, G4), for the report.
    public required IReadOnlyDictionary<string, string> BlockedProjects { get; init; }
}
