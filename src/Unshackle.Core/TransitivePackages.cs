using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// TAAK-329, council round 16 G10: a project that drops the old package also drops every package that came only with it. When the project
// (or a project referencing it) uses types of such a package, it gets a direct PackageReference pinned to the version that resolved before,
// so the build resolves what it did. The edits go through the fix-independent project-file pass (IProjectFileMigration), one row each.
internal class TransitivePackages : IProjectFileMigration
{
    private static readonly StringComparer s_pathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private readonly Dictionary<string, IReadOnlyList<ProjectFileEdit>> _edits;

    private TransitivePackages(Dictionary<string, IReadOnlyList<ProjectFileEdit>> edits, IReadOnlyList<(string ProjectPath, ManualResult Result)> manuals)
    {
        _edits = edits;
        Manuals = manuals;
    }

    public bool HasEdits => _edits.Count > 0;

    // Council round 17 T3: lost packages that a project reference brings at another version; Manual rows on the project file, no pin.
    public IReadOnlyList<(string ProjectPath, ManualResult Result)> Manuals { get; }

    public Task<IReadOnlyList<ProjectFileEdit>> EditsForAsync(Project project, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProjectFileEdit>>(project.FilePath is string path && _edits.TryGetValue(path, out IReadOnlyList<ProjectFileEdit>? edits) ? edits : []);

    // The pins per project that drops the removed package. Needs current assets (TAAK-319): without them nothing is known, nothing is added.
    internal static async Task<TransitivePackages> PlanAsync(
        Solution solution, IEnumerable<ProjectMigration> migrations, IPackageAssetsReader reader, CancellationToken cancellationToken)
    {
        var edits = new Dictionary<string, IReadOnlyList<ProjectFileEdit>>(s_pathComparer);
        List<(string, ManualResult)> manuals = [];
        foreach (ProjectMigration migration in migrations.Where(m => !m.KeepRemovedPackage).DistinctBy(m => m.ProjectPath, s_pathComparer))
        {
            PackageAssets assets = reader.Read(migration.ProjectPath);
            if (assets.State != AssetsState.Current)
            {
                continue;
            }

            HashSet<string> direct = new(assets.Requirements.Where(r => r.RequiredBy == "project").Select(r => r.Package), StringComparer.OrdinalIgnoreCase);
            List<string> removed = [.. new[] { migration.Package.RemovedPackage }.Concat(migration.Package.AlsoRemovedPackages).Where(direct.Contains)];
            if (removed.Count == 0)
            {
                continue;
            }

            HashSet<string> added = new(migration.Package.AddedPackages.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var lost = Lost(assets, removed).Where(p => !added.Contains(p)).ToDictionary(p => p, p => assets.Resolved[p], StringComparer.OrdinalIgnoreCase);
            if (lost.Count == 0)
            {
                continue;
            }

            List<Project> projects = Users(solution, migration.ProjectPath);
            IReadOnlyDictionary<string, IReadOnlyList<string>> used = await UsedAsync(projects, lost, cancellationToken);
            List<ProjectFileEdit> pins = [];
            foreach ((string package, IReadOnlyList<string> users) in used)
            {
                if (OtherVersion(assets, package, lost[package]) is PackageRequirement other)
                {
                    manuals.Add((migration.ProjectPath, new ManualResult(
                        $"{package} {lost[package]} came transitively with {migration.Package.RemovedPackage} and {PackageAddEdit.UserList(users)} "
                        + $"{(users.Count == 1 ? "uses" : "use")} it, but project reference {other.RequiredBy} brings {package} {other.Range}"
                        + $"{Direction(other, lost[package])}; a pin would differ from it. Add the reference by hand with the version you want.")
                    {
                        Dependents = users.Count > PackageAddEdit.MaxUsersInRow ? users : null,
                    }));
                    continue;
                }

                pins.Add(new PackageAddEdit { Name = package, Version = lost[package], KeepsTransitiveOf = migration.Package.RemovedPackage, KeptFor = users });
            }

            if (pins.Count > 0)
            {
                edits[migration.ProjectPath] = pins;
            }
        }

        return new TransitivePackages(edits, manuals);
    }

    // The resolved packages reachable only through the removed ones: not declared, and not reachable from any other direct package.
    internal static IReadOnlyList<string> Lost(PackageAssets assets, IEnumerable<string> removed)
    {
        ArgumentNullException.ThrowIfNull(assets);
        Dictionary<string, List<string>> graph = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> direct = new(StringComparer.OrdinalIgnoreCase);
        foreach (PackageRequirement requirement in assets.Requirements)
        {
            if (requirement.RequiredBy == "project")
            {
                direct.Add(requirement.Package);
                continue;
            }

            string by = requirement.RequiredBy.Split('/')[0];
            if (!graph.TryGetValue(by, out List<string>? next))
            {
                graph[by] = next = [];
            }

            next.Add(requirement.Package);
        }

        HashSet<string> roots = new(removed, StringComparer.OrdinalIgnoreCase);
        HashSet<string> kept = Reach(graph, direct.Where(d => !roots.Contains(d)));
        return [.. Reach(graph, roots).Where(p => !roots.Contains(p) && !direct.Contains(p) && !kept.Contains(p) && assets.Resolved.ContainsKey(p)).Order(StringComparer.OrdinalIgnoreCase)];
    }

    // Council round 17 T3: a project reference that requires the package at another (lower bound) version than the pin, or null. The same
    // version is accepted: then the pin only repeats what the reference brings.
    internal static PackageRequirement? OtherVersion(PackageAssets assets, string package, string version)
    {
        ArgumentNullException.ThrowIfNull(assets);
        Version? pinned = PackageRequirement.ParseVersion(version);
        return assets.ProjectReferenceRequirements.FirstOrDefault(r => string.Equals(r.Package, package, StringComparison.OrdinalIgnoreCase) && r.LowerBound != pinned);
    }

    // Council round 19 T4: a differing version stays Manual either way; the reason tells which case applies. A higher reference bound conflicts
    // with the pin (NU1605); a lower one means dropping the pin silently downgrades to that bound. An unparsable bound names neither.
    internal static string Direction(PackageRequirement other, string version)
    {
        ArgumentNullException.ThrowIfNull(other);
        Version? pinned = PackageRequirement.ParseVersion(version);
        return other.LowerBound is not Version bound || pinned is null ? ""
            : bound > pinned ? " (higher: NU1605 risk)"
            : $" (lower: removing the pin downgrades to {bound})";
    }

    private static HashSet<string> Reach(Dictionary<string, List<string>> graph, IEnumerable<string> starts)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        Stack<string> todo = new(starts);
        while (todo.TryPop(out string? package))
        {
            if (seen.Add(package) && graph.TryGetValue(package, out List<string>? next))
            {
                next.ForEach(todo.Push);
            }
        }

        return seen;
    }

    // The project (every target framework) and every project that references it, directly or not.
    private static List<Project> Users(Solution solution, string projectPath)
    {
        List<Project> own = [.. solution.Projects.Where(p => p.FilePath is string path && s_pathComparer.Equals(path, projectPath))];
        ProjectDependencyGraph graph = solution.GetProjectDependencyGraph();
        HashSet<ProjectId> ids = [.. own.Select(p => p.Id), .. own.SelectMany(p => graph.GetProjectsThatTransitivelyDependOnThisProject(p.Id))];
        return [.. solution.Projects.Where(p => ids.Contains(p.Id))];
    }

    // The lost packages whose types (or namespaces found only in them) the code names. An assembly belongs to a package by its path in the
    // NuGet packages folder: <id>/<version>/... An assembly from elsewhere (a framework reference pack) does not count: it stays.
    // Per package every project (in solution order, once per name) whose code uses it (council round 17 T2, round 19 T5: named in the row).
    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> UsedAsync(
        IEnumerable<Project> projects, Dictionary<string, string> lost, CancellationToken cancellationToken)
    {
        SortedDictionary<string, List<string>> used = new(StringComparer.OrdinalIgnoreCase);
        foreach (Project project in projects)
        {
            if (await project.GetCompilationAsync(cancellationToken) is not Compilation compilation)
            {
                continue;
            }

            Dictionary<IAssemblySymbol, string> assemblies = new(SymbolEqualityComparer.Default);
            foreach (PortableExecutableReference reference in compilation.References.OfType<PortableExecutableReference>())
            {
                if (reference.FilePath is string path && PackageOf(path, lost) is string package && compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    assemblies[assembly] = package;
                }
            }

            if (assemblies.Count == 0)
            {
                continue;
            }

            HashSet<string> found = new(StringComparer.OrdinalIgnoreCase);

            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (SimpleNameSyntax name in (await tree.GetRootAsync(cancellationToken)).DescendantNodes().OfType<SimpleNameSyntax>())
                {
                    SymbolInfo info = model.GetSymbolInfo(name, cancellationToken);
                    if ((info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is ISymbol symbol && Package(symbol, assemblies) is string package)
                    {
                        found.Add(package);
                    }
                }
            }

            foreach (string package in found)
            {
                if (!used.TryGetValue(package, out List<string>? names))
                {
                    used[package] = names = [];
                }

                if (!names.Contains(project.Name, StringComparer.Ordinal))
                {
                    names.Add(project.Name);
                }
            }
        }

        return used.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string? Package(ISymbol symbol, Dictionary<IAssemblySymbol, string> assemblies)
    {
        if (symbol is INamespaceSymbol ns)
        {
            // A using of a namespace that only the lost package declares stops compiling too (CS0234).
            List<IAssemblySymbol> declaring = [.. ns.ConstituentNamespaces.Select(n => n.ContainingAssembly).OfType<IAssemblySymbol>()];
            return declaring.Count > 0 && declaring.All(assemblies.ContainsKey) ? assemblies[declaring[0]] : null;
        }

        ISymbol original = symbol is IMethodSymbol { ReducedFrom: IMethodSymbol reduced } ? reduced : symbol.OriginalDefinition;
        return original.ContainingAssembly is IAssemblySymbol assembly && assemblies.TryGetValue(assembly, out string? package) ? package : null;
    }

    // The package of an assembly path: two consecutive segments <id>/<version> of a lost package.
    internal static string? PackageOf(string path, IReadOnlyDictionary<string, string> lost)
    {
        string[] segments = path.Split('/', '\\');
        for (int i = 0; i + 1 < segments.Length; i++)
        {
            if (lost.TryGetValue(segments[i], out string? version) && string.Equals(segments[i + 1], version, StringComparison.OrdinalIgnoreCase))
            {
                return lost.Keys.First(k => string.Equals(k, segments[i], StringComparison.OrdinalIgnoreCase));
            }
        }

        return null;
    }
}
