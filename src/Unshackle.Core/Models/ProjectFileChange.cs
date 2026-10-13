namespace Unshackle.Core.Models;

public class ProjectFileChange
{
    public required string FilePath { get; init; }

    public required string OldText { get; init; }

    public required string NewText { get; init; }

    // A new file (OldText is empty): it must not exist yet, for example the source of a vendored package (TAAK-194).
    public bool Creates { get; init; }

    // A shared file that keeps the removed package because this project on disk (not migrated, e.g. not loaded) still uses it.
    public string? KeptRemovedPackageFor { get; init; }
}
