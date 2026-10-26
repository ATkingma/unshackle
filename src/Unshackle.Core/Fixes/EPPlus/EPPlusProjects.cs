using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus;

// Council round 13, E17 (TAAK-326): EPPlus migrates per project. A project with a blocking row stays whole on EPPlus; a project that
// shares a file with a kept project, or uses or exposes a member of one whose signature names an EPPlus type, stays too
// (EPPlusTypeCrossesProject). The other projects move to ClosedXML with both packages side by side in the solution. No per-file mode.
internal static class EPPlusProjects
{
    public const string KeptPatternId = "epplus.kept";

    private static readonly StringComparer s_pathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static async Task<PartialPlan?> PlanAsync(Solution solution, IReadOnlyList<Finding> roots, CancellationToken cancellationToken)
    {
        // A multi-target project is loaded once per target framework: every copy stays or moves together.
        List<IGrouping<string, Project>> projects =
        [
            .. solution.Projects.Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null).GroupBy(p => p.FilePath!, s_pathComparer),
        ];
        var projectsOfFile = new Dictionary<string, HashSet<string>>(s_pathComparer);
        foreach (IGrouping<string, Project> project in projects)
        {
            foreach (Document document in project.SelectMany(p => p.Documents))
            {
                string path = document.FilePath ?? document.Name;
                if (!projectsOfFile.TryGetValue(path, out HashSet<string>? owners))
                {
                    projectsOfFile[path] = owners = new HashSet<string>(s_pathComparer);
                }

                owners.Add(project.Key);
            }
        }

        // Why each kept project stays: its first root, or the project it is tied to.
        var kept = new Dictionary<string, (EPPlusReason? Code, string Why)>(s_pathComparer);
        foreach (Finding root in roots)
        {
            IEnumerable<string> owners = projects.Any(p => s_pathComparer.Equals(p.Key, root.FilePath)) ? [root.FilePath]
                : projectsOfFile.TryGetValue(root.FilePath, out HashSet<string>? found) ? found : [];
            if (!owners.Any())
            {
                // A root outside every project (a shared props file) has no project to keep: all-or-nothing.
                return null;
            }

            foreach (string owner in owners)
            {
                kept.TryAdd(owner, (null, $"{Where(root)} cannot be migrated"));
            }
        }

        Dictionary<string, Project> withEPPlus = new(s_pathComparer);
        foreach (IGrouping<string, Project> project in projects)
        {
            if (await project.First().GetCompilationAsync(cancellationToken) is Compilation compilation && compilation.ReferencedAssemblyNames.Any(IsEPPlus))
            {
                withEPPlus[project.Key] = project.First();
            }
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach ((string path, HashSet<string> owners) in projectsOfFile.Where(f => f.Value.Count > 1 && f.Value.Any(kept.ContainsKey)))
            {
                string keeper = owners.First(kept.ContainsKey);
                foreach (string owner in owners.Where(o => !kept.ContainsKey(o)))
                {
                    kept[owner] = (null, $"it shares {Path.GetFileName(path)} with {Path.GetFileName(keeper)}, which stays on EPPlus");
                    changed = true;
                }
            }

            foreach ((string user, Project project) in withEPPlus)
            {
                foreach ((string declaring, string member) in await CrossingsAsync(project, projectsOfFile, cancellationToken))
                {
                    if (declaring == user || kept.ContainsKey(user) == kept.ContainsKey(declaring))
                    {
                        continue;
                    }

                    string moving = kept.ContainsKey(user) ? declaring : user;
                    kept[moving] = (EPPlusReason.EPPlusTypeCrossesProject,
                        $"{member} names an EPPlus type and is used across {Path.GetFileName(user)} and {Path.GetFileName(declaring)}, and the other project stays on EPPlus");
                    changed = true;
                }
            }
        }

        if (withEPPlus.Keys.All(kept.ContainsKey))
        {
            return null;
        }

        Solution planned = solution;
        foreach (Document document in projects.Where(p => kept.ContainsKey(p.Key)).SelectMany(p => p).SelectMany(p => p.Documents))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            planned = planned.WithDocumentSyntaxRoot(document.Id, root.WithAdditionalAnnotations(new SyntaxAnnotation(PartialPlan.KeptAnnotationKind)));
        }

        List<Finding> rows = [.. roots];
        foreach ((string project, (EPPlusReason? code, string why)) in kept.Where(k => withEPPlus.ContainsKey(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            string reason = $"the whole project stays on EPPlus (no per-file mode): {why}. Migrate it by hand, or fix that and run the fix again.";
            rows.Add(new Finding
            {
                FilePath = project,
                Line = 0,
                FixId = "epplus",
                PatternId = KeptPatternId,
                Result = new ManualResult(reason) { Kept = KeptReason.EPPlusProjectKept, Code = code?.ToString() },
            });
        }

        return new PartialPlan { Solution = planned, Findings = rows };
    }

    // Members this project uses that another project of the solution declares with an EPPlus type in their signature: (declaring project, member).
    private static async Task<List<(string Declaring, string Member)>> CrossingsAsync(
        Project project, Dictionary<string, HashSet<string>> projectsOfFile, CancellationToken cancellationToken)
    {
        List<(string, string)> found = [];
        foreach (Document document in project.Documents)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
            {
                continue;
            }

            foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (model.GetSymbolInfo(name, cancellationToken).Symbol is not ISymbol symbol || symbol.Locations.FirstOrDefault(l => l.IsInSource) is not Location location
                    || !NamesEPPlus(symbol) || !projectsOfFile.TryGetValue(location.SourceTree!.FilePath, out HashSet<string>? declaring))
                {
                    continue;
                }

                found.AddRange(declaring.Select(d => (d, symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat))));
            }
        }

        return found;
    }

    private static bool NamesEPPlus(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => IsEPPlus(method.ReturnType) || method.Parameters.Any(p => IsEPPlus(p.Type)),
        IPropertySymbol property => IsEPPlus(property.Type) || property.Parameters.Any(p => IsEPPlus(p.Type)),
        IFieldSymbol field => IsEPPlus(field.Type),
        IEventSymbol @event => IsEPPlus(@event.Type),
        INamedTypeSymbol type => type.AllInterfaces.Any(IsEPPlus) || (type.BaseType is { } baseType && IsEPPlus(baseType)),
        _ => false,
    };

    private static bool IsEPPlus(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => IsEPPlus(array.ElementType),
        IPointerTypeSymbol pointer => IsEPPlus(pointer.PointedAtType),
        INamedTypeSymbol named => (named.ContainingAssembly is { } assembly && IsEPPlus(assembly.Identity)) || named.TypeArguments.Any(IsEPPlus),
        _ => false,
    };

    private static bool IsEPPlus(AssemblyIdentity assembly) => assembly.Name.StartsWith("EPPlus", StringComparison.OrdinalIgnoreCase);

    private static string Where(Finding finding) => finding.Line > 0 ? $"{Path.GetFileName(finding.FilePath)}:{finding.Line}" : Path.GetFileName(finding.FilePath);
}
