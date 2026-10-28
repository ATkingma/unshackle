namespace Unshackle.Core.Models;

// A same-name version bump (council round 18, X3): a PackageReference of Name whose declared version is a plain version at least From and
// below To gets To. The version is edited where it is declared: on the reference, in a shared Directory.Build.props (G3), or in the central
// <PackageVersion> of Directory.Packages.props, the last only when every loaded project under that file that references the package is
// unblocked; otherwise an Info row names the blocking projects. A version below From, at least To, or not a plain version is left alone.
public class PackageVersionEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public required string From { get; init; }

    public required string To { get; init; }

    // Council round 20, X9: when set, a plain version below From is not silently left alone but kept with this Info row
    // (an old major the fix will not raise itself, so the user updates it by hand).
    public string? BelowFromInfo { get; init; }

    // Council round 25, X15: when set, a bump that would apply is not made but reported as Manual with this reason (the fix cannot tell whether it is needed).
    public string? ManualInstead { get; init; }

    // Council round 25, X15: when set, a bump that would apply is not made but kept with this Info row (recommended, not needed).
    public string? InfoInstead { get; init; }

    // Council round 25, X15: a central version kept for another project is Manual instead of Info (the project fails to restore without the bump, NU1605).
    public bool CentralKeptIsManual { get; init; }

    public override string Describe() => $"PackageReference {Name} {From} or later → {To}";
}
