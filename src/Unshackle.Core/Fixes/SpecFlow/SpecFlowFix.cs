using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.SpecFlow.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow;

/// <summary>
/// SpecFlow 3.x → Reqnroll (TAAK-340). An EOL fix (council round 8, S1): SpecFlow is abandoned since 2024-12-31, Reqnroll is its
/// maintained BSD-3-Clause fork. Packages are renamed in place, namespaces swapped, the few renamed types renamed.
/// </summary>
public class SpecFlowFix : IFix, IProjectFileMigration, IPartialFix
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
    // blocker naming the package, seen in a reference path or (build-only packages, council round 21 S11) in the project file.
    public static IReadOnlyList<string> UnportedPlugins { get; } =
        ["specflow.plus.livingdocplugin", "specflow.allure", "specrun.", "specflow.assist.dynamic"];

    // Packages that nothing else depends on: a project that resolves one declares it itself (or through a shared file). Council round 18,
    // S9: when no file declares it statically, the rename is Manual instead of silently skipped.
    private static readonly HashSet<string> s_directPackages =
        new(["SpecFlow.NUnit", "SpecFlow.xUnit", "SpecFlow.MsTest", "SpecFlow.Autofac"], StringComparer.OrdinalIgnoreCase);

    // The assemblies of the SpecFlow package (= its runtime and its container).
    private static readonly HashSet<string> s_oldAssemblies = new(["TechTalk.SpecFlow", "BoDi", "SpecFlow.Autofac.SpecFlowPlugin"], StringComparer.OrdinalIgnoreCase);

    private readonly IProjectProperties _properties;

    public SpecFlowFix(IProjectProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        _properties = properties;
    }

    public string Id => "specflow";

    // Every namespace of TechTalk.SpecFlow 3.9.74 and BoDi; the patterns swap them, the Fixer removes one only where nothing binds to it.
    public IReadOnlyList<string> RemovedNamespaces { get; } =
    [
        "TechTalk.SpecFlow", "TechTalk.SpecFlow.Assist", "TechTalk.SpecFlow.Assist.Attributes", "TechTalk.SpecFlow.Assist.ValueComparers",
        "TechTalk.SpecFlow.Assist.ValueRetrievers", "TechTalk.SpecFlow.Bindings", "TechTalk.SpecFlow.Bindings.Discovery", "TechTalk.SpecFlow.Bindings.Reflection",
        "TechTalk.SpecFlow.Configuration", "TechTalk.SpecFlow.Events", "TechTalk.SpecFlow.Infrastructure", "TechTalk.SpecFlow.Plugins",
        "TechTalk.SpecFlow.TestFramework", "TechTalk.SpecFlow.Tracing", "TechTalk.SpecFlow.UnitTestProvider", "BoDi", "SpecFlow.Autofac",
        "SpecFlow.Autofac.SpecFlowPlugin",
    ];

    // The patterns write the Reqnroll usings in place of the SpecFlow ones.
    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    public IReadOnlyList<string> OldLibraryAssemblies { get; } = [.. s_oldAssemblies];

    // Stateless patterns.
    public bool FixesFilesInParallel => true;

    // Code moves only together with its packages: a file on Reqnroll in a project still on SpecFlow does not build. A Manual row (a
    // pattern, or a project blocker) keeps its unit on SpecFlow (TAAK-353, council round 18 S2); with no safe plan the solution stays as it is.
    public bool AllOrNothing => true;

    public Task<PartialPlan?> PlanPartialAsync(Solution solution, IReadOnlyList<Finding> roots, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(roots);
        return SpecFlowProjects.PlanAsync(solution, roots, cancellationToken);
    }

    public Task<IReadOnlyList<ManualResult>> ProjectBlockersAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult<IReadOnlyList<ManualResult>>(Blockers(project));
    }

    private List<ManualResult> Blockers(Project project)
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
            // Council round 21, S11: a build-only package has no reference path, so the project file is read too.
            IReadOnlyList<string> declared = project.FilePath is string path ? _properties.PackageReferences(path) ?? [] : [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string plugin in PackageIds(project).Concat(declared).Where(id => UnportedPlugins.Any(p => id.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .Order(StringComparer.OrdinalIgnoreCase).Where(seen.Add))
            {
                blockers.Add((ManualResult)PatternResult.Manual($"The project uses the SpecFlow plugin package {plugin}, which has no Reqnroll port. " +
                    "Replace or remove it by hand (Reqnroll has its own LivingDoc, Allure and runner integrations) and run the fix again."));
            }

            // TAAK-340 step 5: Reqnroll 3.3.4 reads no App.config (ConfigurationLoader.GetAppConfigStub returns the defaults).
            if (project.FilePath is string projectPath && _properties.HasAppConfigSection(projectPath, "specFlow"))
            {
                blockers.Add((ManualResult)PatternResult.Manual("The project has a <specFlow> section in App.config. Reqnroll reads no App.config, so " +
                    "its settings would silently fall back to the defaults. Move them to specflow.json (or reqnroll.json), remove the section and its " +
                    "<configSections> entry, and run the fix again."));
            }

            foreach (string unknown in declared.Where(IsUnknownSpecFlowPackage).Where(seen.Add).Order(StringComparer.OrdinalIgnoreCase))
            {
                blockers.Add((ManualResult)PatternResult.Manual($"The project references {unknown}, a SpecFlow package Unshackle does not know (no Reqnroll " +
                    "counterpart on its list). Replace or remove it by hand and run the fix again."));
            }
        }

        return blockers;
    }

    public Task<IReadOnlyList<ProjectFileEdit>> EditsForAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Task.FromResult(Edits(project));
    }

    private IReadOnlyList<ProjectFileEdit> Edits(Project project)
    {
        if (!OldReferencePaths(project).Any())
        {
            return [];
        }

        HashSet<string> used = PackageIds(project);
        return
        [
            .. RenamedPackages.Select(p => new PackageRenameEdit
            {
                OldName = p.Old,
                NewName = p.New,
                Version = ReqnrollVersion,
                MustBeDeclared = s_directPackages.Contains(p.Old) && used.Contains(p.Old),
            }),
            // <Using Include="TechTalk.SpecFlow" /> items: the code patterns never see the global usings MSBuild generates from them.
            .. RemovedNamespaces.Select(ns => new UsingRenameEdit { OldNamespace = ns, NewNamespace = SpecFlowNames.NewNamespace(ns)! }),
        ];
    }

    // Council round 23, S14: specflow.json keeps its name (the Reqnroll runtime and generator both fall back to it); no file edit.
    public Task<IReadOnlyList<string>> ProjectInfoAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!OldReferencePaths(project).Any())
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        List<string> info =
        [
            "specflow.json kept (Reqnroll reads it); rename by hand together with its csproj items; the generator lists only reqnroll.json " +
                "for VS up-to-date checks.",
        ];
        // TAAK-340 step 5: code-behind the SpecFlow generator wrote; the Reqnroll generator overwrites it next to the .feature file on build.
        List<string> codeBehind = [.. project.Documents.Select(d => d.FilePath).OfType<string>()
            .Where(p => p.EndsWith(".feature.cs", StringComparison.OrdinalIgnoreCase)).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];
        if (codeBehind.Count > 0)
        {
            info.Add($"{codeBehind.Count} *.feature.cs code-behind file(s) ({string.Join(", ", codeBehind.Take(5))}{(codeBehind.Count > 5 ? ", ..." : "")}) " +
                "were generated by SpecFlow; the Reqnroll generator rewrites them on the next build. If they are checked in, commit the regenerated files.");
        }

        return Task.FromResult<IReadOnlyList<string>>(info);
    }

    public IReadOnlyList<IPattern> Patterns { get; } =
        // NamespacePattern last: the patterns before it need the SpecFlow usings to bind the old types.
        [new RenamedTypePattern(), new CombinedTagsPattern(), new PendingPattern(), new StringStepCallPattern(), new AsyncVoidBindingPattern(), new StepExpressionPattern(), new NamespacePattern()];

    internal static bool ReferencesSpecFlow(Project project) => OldReferencePaths(project).Any();

    private static IEnumerable<string> OldReferencePaths(Project project) =>
        project.MetadataReferences.OfType<PortableExecutableReference>()
            .Select(r => r.FilePath)
            .OfType<string>()
            .Where(path => s_oldAssemblies.Contains(Path.GetFileNameWithoutExtension(path)));

    // Council round 21, S11: a SpecFlow.* / SpecRun.* package that is neither renamed nor a known unported plugin.
    internal static bool IsUnknownSpecFlowPackage(string id) =>
        (id.StartsWith("SpecFlow.", StringComparison.OrdinalIgnoreCase) || id.StartsWith("SpecRun.", StringComparison.OrdinalIgnoreCase))
        && !RenamedPackages.Any(p => p.Old.Equals(id, StringComparison.OrdinalIgnoreCase))
        && !UnportedPlugins.Any(p => id.StartsWith(p, StringComparison.OrdinalIgnoreCase));

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
