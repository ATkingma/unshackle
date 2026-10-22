namespace Unshackle.Core.Models;

/// <summary>
/// The restored packages of one project (TAAK-319), from <c>obj/project.assets.json</c>: what was resolved, and what each package and the
/// project itself require. Only meaningful when <see cref="State"/> is <see cref="AssetsState.Current"/>.
/// </summary>
public class PackageAssets
{
    /// <summary>No assets: nothing is known.</summary>
    public static PackageAssets Missing { get; } = new() { State = AssetsState.Missing, Resolved = new Dictionary<string, string>(), Requirements = [] };

    public required AssetsState State { get; init; }

    /// <summary>Package id (case-insensitive) → resolved version; the highest over all target frameworks.</summary>
    public required IReadOnlyDictionary<string, string> Resolved { get; init; }

    /// <summary>Every requirement on a package: by the project (direct) or by a resolved package, with its version range as written.</summary>
    public required IReadOnlyList<PackageRequirement> Requirements { get; init; }

    /// <summary>True when the package resolved to <paramref name="version"/> or higher in some target framework.</summary>
    public bool ResolvesAtLeast(string package, Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return Resolved.TryGetValue(package, out string? resolved) && PackageRequirement.ParseVersion(resolved) is Version found && found >= version;
    }

    /// <summary>
    /// True when the package resolves to <paramref name="version"/> or higher, or anything requires at least that version (the lower bound of
    /// its range), or a requirement cannot be read: then a pin below it is not safe.
    /// </summary>
    public bool RequiresAtLeast(string package, Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return ResolvesAtLeast(package, version)
            || Requirements.Any(r => string.Equals(r.Package, package, StringComparison.OrdinalIgnoreCase) && (r.LowerBound is not Version lower || lower >= version));
    }
}
