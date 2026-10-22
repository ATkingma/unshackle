namespace Unshackle.Core.Models;

// The solution-level removal of an old <PackageVersion> (council round 12, G7). Made by the Fixer after every project's edits, never by a fix.
public class PackageVersionRemoveEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public override string Describe() => $"PackageVersion {Name} removed";
}
