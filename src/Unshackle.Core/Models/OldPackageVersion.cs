namespace Unshackle.Core.Models;

// A <PackageVersion> of a renamed or removed package that may go once every project is edited (council round 12, G7).
public class OldPackageVersion
{
    // The Directory.Packages.props (full path).
    public required string PropsPath { get; init; }

    public required string Package { get; init; }
}
