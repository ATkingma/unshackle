using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.SpecFlow.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow;

/// <summary>
/// SpecFlow 3.x → Reqnroll (TAAK-340). An EOL fix (council round 8, S1): SpecFlow is abandoned since 2024-12-31, Reqnroll is its
/// maintained BSD-3-Clause fork. Packages are renamed in place, namespaces swapped, the few renamed types renamed.
/// </summary>
public class SpecFlowFix : IFix, IProjectFileMigration
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, BSD-3-Clause.
    public const string ReqnrollVersion = "3.3.4";

    // SpecFlow package → Reqnroll package (same role). The runner packages bring Reqnroll and its generator along.
    public static IReadOnlyList<(string Old, string New)> RenamedPackages { get; } =
    [
        ("SpecFlow", "Reqnroll"),
        ("SpecFlow.NUnit", "Reqnroll.NUnit"),
        ("SpecFlow.xUnit", "Reqnroll.xUnit"),
        ("SpecFlow.MsTest", "Reqnroll.MsTest"),
        ("SpecFlow.Tools.MsBuild.Generation", "Reqnroll.Tools.MsBuild.Generation"),
        // Council round 18, S8: the one plugin with a Reqnroll port; its namespace SpecFlow.Autofac becomes Reqnroll.Autofac (SpecFlowNames).
        ("SpecFlow.Autofac", "Reqnroll.Autofac"),
    ];

    // Council round 18, S8: plugins without a Reqnroll port, as package id prefixes (lower case, as in the NuGet folder). Each is a project
    // blocker naming the package. Only packages with an assembly are seen (a build-only package has no reference path).
    public static IReadOnlyList<string> UnportedPlugins { get; } =
        ["specflow.plus.livingdocplugin", "specflow.allure", "specrun.", "specflow.assist.dynamic"];

    // Packages that nothing else depends on: a project that resolves one declares it itself (or through a shared file). Council round 18,
    // S9: when no file declares it statically, the rename is Manual instead of silently skipped.
    private static readonly HashSet<string> s_directPackages =
        new(["SpecFlow.NUnit", "SpecFlow.xUnit", "SpecFlow.MsTest", "SpecFlow.Autofac"], StringComparer.OrdinalIgnoreCase);

    // The assemblies of the SpecFlow package (= its runtime and its container).
    private static readonly HashSet<string> s_oldAssemblies = new(["TechTalk.SpecFlow", "BoDi", "SpecFlow.Autofac.SpecFlowPlugin"], StringComparer.OrdinalIgnoreCase);

    public string Id => "specflow";

    // Every namespace of TechTalk.SpecFlow 3.9.74 and BoDi; the patterns swap them, the Fixer removes one only where nothing binds to it.
    public IReadOnlyList<string> RemovedNamespaces { get; } =
    [
        "TechTalk.SpecFlow", "TechTalk.SpecFlow.Assist", "TechTalk.SpecFlow.Assist.Attributes", "TechTalk.SpecFlow.Assist.ValueComparers",
        "TechTalk.SpecFlow.Assist.ValueRetrievers", "TechTalk.SpecFlow.Bindings", "TechTalk.SpecFlow.Bindings.Discovery", "TechTalk.SpecFlow.Bindings.Reflection",
        "TechTalk.SpecFlow.Configuration", "TechTalk.SpecFlow.Events", "TechTalk.SpecFlow.Infrastructure", "TechTalk.SpecFlow.Plugins",
        "TechTalk.SpecFlow.TestFramework", "TechTalk.SpecFlow.Tracing", "TechTalk.SpecFlow.UnitTestProvider", "BoDi", "SpecFlow.Autofac",
    ];

    // The patterns write the Reqnroll usings in place of the SpecFlow ones.
    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    public IReadOnlyList<string> OldLibraryAssemblies { get; } = [.. s_oldAssemblies];

    // Stateless patterns.
    public bool FixesFilesInParallel => true;

    // Code moves only together with its packages: a file on Reqnroll in a project still on SpecFlow does not build. Any Manual row
    // (a pattern, or a project blocker) leaves the solution as it is (conservative; council question 6 in TAAK-340).
    public bool AllOrNothing => true;

    public Task<IReadOnlyList<ManualResult>> ProjectBlockersAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult<IReadOnlyList<ManualResult>>(Blockers(project));
    }

    private static List<ManualResult> Blockers(Project project)
    {
        List<ManualResult> blockers = [];
        // packages.config: the package swap is not an edit of the project file (survey: 3 of 13 repos). Manual until a packages.config edit exists.
        if (OldReferencePaths(project).Any(IsPackagesFolderPath))
        {
            blockers.Add((ManualResult)PatternResult.Manual("The project references SpecFlow through packages.config. Unshackle only renames PackageReference items: " +
                "switch to PackageReference (or swap the packages by hand) and run the fix again."));
        }

        if (OldReferencePaths(project).Any())
        {
            foreach (string plugin in PackageIds(project).Where(id => UnportedPlugins.Any(p => id.StartsWith(p, StringComparison.OrdinalIgnoreCase))).Order(StringComparer.Ordinal))
            {
                blockers.Add((ManualResult)PatternResult.Manual($"The project uses the SpecFlow plugin package {plugin}, which has no Reqnroll port. " +
                    "Replace or remove it by hand (Reqnroll has its own LivingDoc, Allure and runner integrations) and run the fix again."));
            }
        }

        return blockers;
    }

    public Task<IReadOnlyList<ProjectFileEdit>> EditsForAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult(Edits(project));
    }

    private static IReadOnlyList<ProjectFileEdit> Edits(Project project)
    {
        if (!OldReferencePaths(project).Any())
        {
            return [];
        }

        HashSet<string> used = PackageIds(project);
        return [.. RenamedPackages.Select(p => new PackageRenameEdit
        {
            OldName = p.Old,
            NewName = p.New,
            Version = ReqnrollVersion,
            MustBeDeclared = s_directPackages.Contains(p.Old) && used.Contains(p.Old),
        })];
    }

    public IReadOnlyList<IPattern> Patterns { get; } =
        // NamespacePattern last: the patterns before it need the SpecFlow usings to bind the old types.
        [new RenamedTypePattern(), new CombinedTagsPattern(), new PendingPattern(), new StringStepCallPattern(), new AsyncVoidBindingPattern(), new NamespacePattern()];

    private static IEnumerable<string> OldReferencePaths(Project project) =>
        project.MetadataReferences.OfType<PortableExecutableReference>()
            .Select(r => r.FilePath)
            .OfType<string>()
            .Where(path => s_oldAssemblies.Contains(Path.GetFileNameWithoutExtension(path)));

    // The NuGet package ids of the project's references (<root>/<id>/<version>/lib/<tfm>/<file>.dll), as written in the path.
    internal static HashSet<string> PackageIds(Project project) =>
        new(project.MetadataReferences.OfType<PortableExecutableReference>().Select(r => r.FilePath).OfType<string>().Select(PackageIdOf).OfType<string>(),
            StringComparer.OrdinalIgnoreCase);

    internal static string? PackageIdOf(string path)
    {
        string[] parts = path.Replace('\\', '/').Split('/');
        int lib = Array.FindLastIndex(parts, p => p.Equals("lib", StringComparison.OrdinalIgnoreCase));
        return lib >= 2 ? parts[lib - 2] : null;
    }

    // packages.config restores to <solution>/packages/<Id>.<Version>/lib/...; PackageReference to <nuget>/<id>/<version>/lib/....
    internal static bool IsPackagesFolderPath(string path) =>
        path.Replace('\\', '/').Contains("/packages/SpecFlow.", StringComparison.OrdinalIgnoreCase)
        || path.Replace('\\', '/').Contains("/packages/BoDi.", StringComparison.OrdinalIgnoreCase);
}
