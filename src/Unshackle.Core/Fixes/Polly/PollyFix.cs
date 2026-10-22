using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly;

// Polly v7 API → Polly v8 resilience pipelines through Microsoft.Extensions.Resilience (council round 8, P1): Polly then is
// only a transitive dependency of a Microsoft package, which the OSMF exempts (TAAK-308 step 1).
public class PollyFix : IFix
{
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

    public IReadOnlyList<IPattern> Patterns { get; } = [new WaitAndRetryAsyncPattern()];
}
