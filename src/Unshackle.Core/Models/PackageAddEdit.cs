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

    // Council round 17 T2: the user project whose code uses the package (the first one if several); set with KeepsTransitiveOf.
    public string? KeptFor { get; init; }

    // Council round 17 T1: a pin is not checked against the new package's closure, so the row starts with the NU1605 hint and its remedy.
    public override string Describe() => KeepsTransitiveOf is string removed
        ? $"may conflict (NU1605): PackageReference {Name} pinned to {Version}, kept for `{KeptFor ?? "?"}` (it came transitively with {removed}). "
            + $"If restore reports NU1605 for {Name}, raise the pin"
        : $"PackageReference {Name} {Version} added";
}
