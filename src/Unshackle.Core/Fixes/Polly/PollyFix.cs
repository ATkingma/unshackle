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

    public string Id => "polly";

    // Polly v7 and Polly.Core share the namespaces Polly and Polly.Retry, so no using is removed: the rewritten code binds to them.
    public IReadOnlyList<string> RemovedNamespaces { get; } = [];

    // Each rewrite names the namespaces it needs; an unchanged file gets nothing.
    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // Stateless patterns.
    public bool FixesFilesInParallel => true;

    // Council round 8, general rule: the package goes per project only at 0 Manual rows. A Manual row leaves v7 code that binds
    // to the Polly assembly, and such code keeps the removed package (ProjectPackages), so no AllOrNothing is needed.
    public PackageMigration Package { get; } = new()
    {
        RemovedPackage = "Polly",
        // Both build on Polly v7; left behind they keep the fee-relevant direct reference.
        AlsoRemovedPackages = ["Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly"],
        AddedPackages = [new() { Name = "Microsoft.Extensions.Resilience", Version = ResilienceVersion }],
    };

    // Council round 10, P9/G4: a target framework Microsoft.Extensions.Resilience does not support blocks the project's file edits.
    // The patterns report the same code on each rewrite in that project, so its code stays on v7 and keeps building.
    public IReadOnlyList<ManualResult> ProjectBlockers(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return PollyTargetFramework.Blockers(project);
    }

    public IReadOnlyList<ProjectFileEdit> EditsFor(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return [];
    }

    // Package references show up as metadata references to the package's assembly; a Polly.Core-only (v8) project is not ours.
    internal static bool ReferencesPollyV7(Project project) =>
        project.MetadataReferences.OfType<PortableExecutableReference>()
            .Any(r => r.FilePath is string path && s_v7Assemblies.Contains(Path.GetFileNameWithoutExtension(path)));

    public IReadOnlyList<IPattern> Patterns { get; } = [new WaitAndRetryAsyncPattern(), new RetryVariantsPattern()];
}
