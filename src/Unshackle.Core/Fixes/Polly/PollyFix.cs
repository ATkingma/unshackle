using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly;

// Polly v7 API → Polly v8 resilience pipelines through Microsoft.Extensions.Resilience (council round 8, P1): Polly then is
// only a transitive dependency of a Microsoft package, which the OSMF exempts (TAAK-308 step 1).
public class PollyFix : IFix, IProjectFileMigration
{
    // Assemblies (= package ids) of the v7 API; a project referencing one is migrated.
    private static readonly HashSet<string> s_v7Assemblies = new(["Polly", "Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly"], StringComparer.OrdinalIgnoreCase);

    // The version our proofs compile every rewrite against (Directory.Packages.props): MIT, brings Polly.Core 8.
    public const string ResilienceVersion = "10.11.0";

    // The Polly.Core that Microsoft.Extensions.Resilience 10.11.0 brings in (via Polly.Extensions 8.4.2, checked in the nuspecs).
    public const string BundledPollyCoreVersion = "8.4.2";

    private readonly IPackageAssetsReader? _assetsReader;

    public PollyFix()
        : this(null)
    {
    }

    // TAAK-354: the restored assets add the declared (central) versions and the consumers' packages to the downgrade check (B5, B6).
    public PollyFix(IPackageAssetsReader? assetsReader)
    {
        _assetsReader = assetsReader;
    }

    public string Id => "polly";

    // Polly v7 and Polly.Core share the namespaces Polly and Polly.Retry, which the rewritten code binds to; only the HttpPolicyExtensions
    // namespace of Polly.Extensions.Http goes (left behind it does not bind once the package is gone).
    public IReadOnlyList<string> RemovedNamespaces { get; } = ["Polly.Extensions.Http"];

    // Each rewrite names the namespaces it needs; an unchanged file gets nothing.
    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // The pre-filter matches the v7 assemblies (council round 12, G9): RemovedNamespaces lacks Polly, which Polly.Core shares.
    public IReadOnlyList<string> OldLibraryAssemblies { get; } = [.. s_v7Assemblies];

    // Stateless patterns.
    public bool FixesFilesInParallel => true;

    // Council round 8, general rule: the package goes per project only at 0 Manual rows. A Manual row leaves v7 code that binds
    // to the Polly assembly, and such code keeps the removed package (ProjectPackages), so no AllOrNothing is needed.
    public PackageMigration Package { get; } = new()
    {
        RemovedPackage = "Polly",
        // Both build on Polly v7; left behind they keep the fee-relevant direct reference.
        AlsoRemovedPackages = ["Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly"],
        AddedPackages =
        [
            new() { Name = "Microsoft.Extensions.Resilience", Version = ResilienceVersion },
            // AddResilienceHandler, only where the code had an HttpClient handler (queue row 6).
            new() { Name = "Microsoft.Extensions.Http.Resilience", Version = ResilienceVersion, OnlyWithCallTo = ["AddPolicyHandler", "AddTransientHttpErrorPolicy"] },
        ],
    };

    // Council round 10, P9/G4: a target framework Microsoft.Extensions.Resilience does not support blocks the project's file edits.
    // TAAK-320: the Fixer then also leaves that project's documents unrewritten, so its code stays on v7 and keeps building.
    public Task<IReadOnlyList<ManualResult>> ProjectBlockersAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult(Blockers(project));
    }

    private static IReadOnlyList<ManualResult> Blockers(Project project)
    {
        // Corpus B1/B4: an older resolved Microsoft.Extensions.* package than the Resilience closure needs blocks the project too.
        return [.. PollyTargetFramework.Blockers(project), .. PollyPackageDowngrade.Blockers(project, _assetsReader)];
    }

    public Task<IReadOnlyList<ProjectFileEdit>> EditsForAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult(Edits(project));
    }

    private static IReadOnlyList<ProjectFileEdit> Edits(Project project)
    {
        // Council round 10, P8: a direct Polly.Core reference goes once Microsoft.Extensions.Resilience brings it in, only when the
        // declared version is at most the bundled one (the engine keeps a higher or unknown one with an Info row). The add is a
        // no-op where the package migration already added it; both are blocked while the project has blocking rows (G4).
        return ReferencesPollyV7(project)
            ? [new PackageAddEdit { Name = "Microsoft.Extensions.Resilience", Version = ResilienceVersion }, new PackageRemoveEdit { Name = "Polly.Core", AtMostVersion = BundledPollyCoreVersion }]
            : [];
    }

    // Package references show up as metadata references to the package's assembly; a Polly.Core-only (v8) project is not ours.
    internal static bool ReferencesPollyV7(Project project) =>
        project.MetadataReferences.OfType<PortableExecutableReference>()
            .Any(r => r.FilePath is string path && s_v7Assemblies.Contains(Path.GetFileNameWithoutExtension(path)));

    public IReadOnlyList<IPattern> Patterns { get; } = [new WaitAndRetryAsyncPattern(), new RetryVariantsPattern(), new WrapPattern(), new AddPolicyHandlerPattern(), new SyncRetryPattern(), new TimeoutPattern(), new NoOpPattern(), new CircuitBreakerPattern(), new UnsupportedPattern()];
}
