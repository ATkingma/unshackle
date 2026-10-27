namespace Unshackle.Core.Models;

// A PackageReference of the project file renamed in place, with the new version; under central package management the version goes
// to a new <PackageVersion> and the old one stays (another project may still use it). Nothing to do when the project does not declare it.
public class PackageRenameEdit : ProjectFileEdit
{
    public required string OldName { get; init; }

    public required string NewName { get; init; }

    public required string Version { get; init; }

    // Council round 18, S9: the project is known to use the package (its assemblies are referenced). When neither the project file nor its
    // Directory.Build.props / Directory.Packages.props declares it statically (an imported file, a property-driven name, a ProjectReference),
    // the edit is Manual instead of skipped: code on the new library with the old package does not build.
    public bool MustBeDeclared { get; init; }

    public override string Describe() => $"PackageReference {OldName} → {NewName} {Version}";
}
