using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly;

// Corpus B1/B4 (CORPUSRUN-polly): Microsoft.Extensions.(Http.)Resilience 10.11.0 pulls in newer Microsoft.Extensions.* packages. A
// project that resolves an older one (a direct reference or a central pin) then fails restore (NU1605 downgrade, NU1109) or build
// (CS0433: Microsoft.Extensions.DependencyInjection 5.0.0 next to DependencyInjection.Abstractions 8 both define ServiceCollection).
// Bumping the user's references is not provably safe (their own code and other packages may rely on that version), so the project
// is blocked with code PackageDowngrade: no file edits, documents stay on v7 (TAAK-320), and the report names each package.
// The resolved versions come from the package paths of the metadata references (<packages>/<id>/<version>/lib/<tfm>/<id>.dll).
internal static class PollyPackageDowngrade
{
    // Minimum versions per dependency group of the Resilience 10.11.0 closure (nuspecs: Resilience, Http.Resilience, Telemetry(.Abstractions),
    // Compliance.Abstractions, AmbientMetadata.Application, DependencyInjection.AutoActivation, ExceptionSummarization, Http.Diagnostics).
    // net462, netstandard2.0 and net8.0 share the 8.0 group.
    private static readonly Dictionary<string, string> s_net8Minimums = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.Extensions.Configuration"] = "8.0.0",
        ["Microsoft.Extensions.DependencyInjection.Abstractions"] = "8.0.2",
        ["Microsoft.Extensions.Diagnostics"] = "8.0.1",
        ["Microsoft.Extensions.Hosting.Abstractions"] = "8.0.1",
        ["Microsoft.Extensions.Http"] = "8.0.1",
        ["Microsoft.Extensions.Logging.Abstractions"] = "8.0.3",
        ["Microsoft.Extensions.Logging.Configuration"] = "8.0.1",
        ["Microsoft.Extensions.ObjectPool"] = "8.0.32",
        ["Microsoft.Extensions.Options"] = "8.0.2",
        ["Microsoft.Extensions.Options.ConfigurationExtensions"] = "8.0.0",
    };

    // The packages released with Resilience (dotnet/extensions); the closure needs exactly this version.
    private static readonly HashSet<string> s_resilienceFamily = new([
        "Microsoft.Extensions.AmbientMetadata.Application", "Microsoft.Extensions.Compliance.Abstractions", "Microsoft.Extensions.Compliance.Redaction",
        "Microsoft.Extensions.DependencyInjection.AutoActivation", "Microsoft.Extensions.Diagnostics.ExceptionSummarization", "Microsoft.Extensions.Http.Diagnostics",
        "Microsoft.Extensions.Telemetry", "Microsoft.Extensions.Telemetry.Abstractions"], StringComparer.OrdinalIgnoreCase);

    // Removed by the migration (Http.Polly) or the packages being added themselves: not a conflict.
    private static readonly HashSet<string> s_ignored = new(["Microsoft.Extensions.Http.Polly", "Microsoft.Extensions.Resilience", "Microsoft.Extensions.Http.Resilience"], StringComparer.OrdinalIgnoreCase);

    // The minimum version the Resilience closure needs of a Microsoft.Extensions.* package for the project's dependency group. A package
    // outside the table gets the group's major (x.0.0): an older major next to the closure risks CS0433 duplicates (B4).
    public static Version Minimum(string id, int groupMajor)
    {
        if (s_resilienceFamily.Contains(id))
        {
            return Version.Parse(PollyFix.ResilienceVersion);
        }

        return groupMajor switch
        {
            >= 10 => s_net8Minimums.ContainsKey(id) ? new Version(10, 0, 13) : new Version(10, 0, 0),
            9 => s_net8Minimums.ContainsKey(id) ? new Version(9, 0, 21) : new Version(9, 0, 0),
            _ => s_net8Minimums.TryGetValue(id, out string? minimum) ? Version.Parse(minimum) : new Version(8, 0, 0),
        };
    }

    // The dependency group NuGet picks from the project's preprocessor symbols: net10.0, net9.0, else the 8.0 group.
    public static int GroupMajor(IEnumerable<string> preprocessorSymbols)
    {
        HashSet<string> symbols = [.. preprocessorSymbols];
        return symbols.Contains("NET10_0_OR_GREATER") ? 10 : symbols.Contains("NET9_0_OR_GREATER") ? 9 : 8;
    }

    // (package id, resolved version) of a NuGet package assembly; null for a framework, project or non-package reference.
    public static (string Id, Version Version)? Resolved(string path)
    {
        string? tfmDirectory = Path.GetDirectoryName(path);
        string? libDirectory = tfmDirectory is null ? null : Path.GetDirectoryName(tfmDirectory);
        string? versionDirectory = libDirectory is null ? null : Path.GetDirectoryName(libDirectory);
        string? idDirectory = versionDirectory is null ? null : Path.GetDirectoryName(versionDirectory);
        if (versionDirectory is null || idDirectory is null)
        {
            return null;
        }

        string id = Path.GetFileName(idDirectory);
        string version = Path.GetFileName(versionDirectory).Split('-', '+')[0];
        return id.Equals(Path.GetFileNameWithoutExtension(path), StringComparison.OrdinalIgnoreCase)
            && id.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase)
            && Version.TryParse(version, out Version? parsed)
            ? (id, parsed)
            : null;
    }

    // "Id resolved < minimum" per package the closure would raise, sorted; empty when none.
    public static IReadOnlyList<string> Conflicts(Project project) => Conflicts(project, PackageAssets.Missing);

    // TAAK-354 (B6): with current assets, the project's own requirements count too. Under central package management these are the
    // PackageVersion pins of Directory.Packages.props; a pin below the closure's minimum fails restore with NU1109 even when the
    // compilation binds the assembly from a framework reference pack (so no package path shows it).
    public static IReadOnlyList<string> Conflicts(Project project, PackageAssets assets)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(assets);
        int groupMajor = GroupMajor(project.ParseOptions?.PreprocessorSymbolNames ?? []);
        IEnumerable<(string Id, Version Version)> resolved = project.MetadataReferences.OfType<PortableExecutableReference>()
            .Select(r => r.FilePath is string path ? Resolved(path) : null)
            .OfType<(string Id, Version Version)>();
        IEnumerable<(string Id, Version Version)> declared = assets.State != AssetsState.Current ? []
            : assets.Requirements
                .Where(r => r.RequiredBy == "project" && r.Package.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.LowerBound is Version lower ? (r.Package, lower) : ((string, Version)?)null)
                .OfType<(string Id, Version Version)>();
        return [.. resolved.Concat(declared)
            .Where(p => !s_ignored.Contains(p.Id) && Normalize(p.Version) < Minimum(p.Id, groupMajor))
            .Select(p => $"{p.Id} {p.Version} (needs >= {Minimum(p.Id, groupMajor)})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    public static IReadOnlyList<ManualResult> Blockers(Project project) => Blockers(project, null);

    // TAAK-354 (B5): every project that references this one, directly or not, gets the Resilience closure too; a consumer that resolves
    // an older Microsoft.Extensions.* package fails restore with NU1605. Such a consumer blocks this project's edits (council: Manual, no bump).
    public static IReadOnlyList<ManualResult> Blockers(Project project, IPackageAssetsReader? assetsReader)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!PollyFix.ReferencesPollyV7(project))
        {
            return [];
        }

        IReadOnlyList<string> conflicts = Conflicts(project, Assets(project, assetsReader));
        if (conflicts.Count > 0)
        {
            return [(ManualResult)PatternResult.Manual(ProjectBlockingReason.PackageDowngrade,
                $"Microsoft.Extensions.Resilience {PollyFix.ResilienceVersion} needs newer Microsoft.Extensions packages than the project resolves: "
                + $"{string.Join(", ", conflicts)}. Raise those references first (restore would fail with NU1605/NU1109, or the build with CS0433), then run the fix again.")];
        }

        ProjectDependencyGraph graph = project.Solution.GetProjectDependencyGraph();
        List<string> consumers = [];
        foreach (ProjectId id in graph.GetProjectsThatTransitivelyDependOnThisProject(project.Id))
        {
            if (project.Solution.GetProject(id) is Project consumer && Conflicts(consumer, Assets(consumer, assetsReader)) is { Count: > 0 } found)
            {
                consumers.Add($"{consumer.Name}: {string.Join(", ", found)}");
            }
        }

        consumers = [.. consumers.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)];
        return consumers.Count == 0
            ? []
            : [(ManualResult)PatternResult.Manual(ProjectBlockingReason.PackageDowngrade,
                $"Microsoft.Extensions.Resilience {PollyFix.ResilienceVersion} would reach projects that reference this one through ProjectReference and resolve older "
                + $"Microsoft.Extensions packages ({string.Join("; ", consumers)}). Raise those references first (restore would fail with NU1605/NU1109), then run the fix again.")];
    }

    private static PackageAssets Assets(Project project, IPackageAssetsReader? assetsReader) =>
        assetsReader is not null && project.FilePath is string path ? assetsReader.Read(path) : PackageAssets.Missing;

    // 8.0 and 8.0.0 compare equal.
    private static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));
}
