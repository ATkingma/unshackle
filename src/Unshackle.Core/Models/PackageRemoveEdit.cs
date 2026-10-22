namespace Unshackle.Core.Models;

// A PackageReference removed from the project file; a <PackageVersion> stays. Nothing to do when the project does not declare it.
public class PackageRemoveEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public override string Describe() => $"PackageReference {Name} removed";
}
