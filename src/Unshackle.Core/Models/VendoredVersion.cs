namespace Unshackle.Core.Models;

// The source of one version of a vendored package, and the packages it depended on (they no longer come in through it).
public class VendoredVersion
{
    // Major.Minor.Build of the package, the same as its assembly version (1.4.0 for 1.4.0.0).
    public required string Version { get; init; }

    public required IReadOnlyList<VendoredFile> Files { get; init; }

    // The version is the one the package asked for; the version the project resolved wins when it can be seen.
    public required IReadOnlyList<AddedPackage> Dependencies { get; init; }
}
