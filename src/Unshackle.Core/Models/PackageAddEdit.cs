namespace Unshackle.Core.Models;

// A PackageReference added to the project file (with a <PackageVersion> under central package management). Nothing to do when the
// project or a file it imports already declares it.
public class PackageAddEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    // TAAK-329, council round 16 G10: set when the package only came transitively with this removed package. The version is the resolved
    // one, so the build resolves what it did before; a central PackageVersion with another version is then Manual instead of kept.
    public string? KeepsTransitiveOf { get; init; }

    public override string Describe() => KeepsTransitiveOf is string removed
        ? $"PackageReference {Name} {Version} added (it came transitively with {removed}, and code uses it)"
        : $"PackageReference {Name} {Version} added";
}
