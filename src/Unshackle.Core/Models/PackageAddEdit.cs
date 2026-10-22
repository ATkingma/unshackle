namespace Unshackle.Core.Models;

// A PackageReference added to the project file (with a <PackageVersion> under central package management). Nothing to do when the
// project or a file it imports already declares it.
public class PackageAddEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    public override string Describe() => $"PackageReference {Name} {Version} added";
}
