namespace Unshackle.Core.Models;

public class ProjectFileChange
{
    public required string FilePath { get; init; }

    public required string OldText { get; init; }

    public required string NewText { get; init; }
}
