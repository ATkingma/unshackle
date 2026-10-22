namespace Unshackle.Core.Models;

// A PackageReference of the project file renamed in place, with the new version; under central package management the version goes
// to a new <PackageVersion> and the old one stays (another project may still use it). Nothing to do when the project does not declare it.
public class PackageRenameEdit : ProjectFileEdit
{
    public required string OldName { get; init; }

    public required string NewName { get; init; }

    public required string Version { get; init; }

    public override string Describe() => $"PackageReference {OldName} → {NewName} {Version}";
}
