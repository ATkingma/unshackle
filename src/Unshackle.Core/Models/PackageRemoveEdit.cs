namespace Unshackle.Core.Models;

// A PackageReference removed from the project file; a <PackageVersion> stays. Nothing to do when the project does not declare it.
public class PackageRemoveEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    // Council round 10, P8: remove only when the declared version (its own or the central one) is at most this version, for example the
    // Polly.Core that Microsoft.Extensions.Resilience brings in; a higher, unknown or range version keeps the reference with an Info row.
    // Null (the default): always remove.
    public string? AtMostVersion { get; init; }

    public override string Describe() => $"PackageReference {Name} removed";
}
