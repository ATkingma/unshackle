namespace Unshackle.Core.Models;

// A known config file next to the project file renamed (for example specflow.json → reqnroll.json), with the project file's items that
// name it exactly (Include, Update, Remove). Nothing to do when the file is not there; Manual when the new name already exists.
public class ConfigFileRenameEdit : ProjectFileEdit
{
    // File names relative to the project folder.
    public required string OldFileName { get; init; }

    public required string NewFileName { get; init; }

    public override string Describe() => $"{OldFileName} renamed to {NewFileName}";
}
