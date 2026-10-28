using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow;

// TAAK-353, council round 18 S2: SpecFlow migrates per unit, a project plus the SpecFlow projects it references (step definitions
// live in binding libraries, and a runner on Reqnroll does not run SpecFlow bindings). A project with a blocking row stays whole on
// SpecFlow, and with it every SpecFlow project it references or that references it, and every project it shares a file with. The
// other projects move to Reqnroll. A blocking row outside every project (a shared props file) keeps the whole solution.
internal static class SpecFlowProjects
{
    public const string KeptPatternId = "specflow.kept";

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

        var kept = new Dictionary<string, string>(s_pathComparer);
        foreach (Finding root in roots)
        {
            IEnumerable<string> owners = projects.Any(p => s_pathComparer.Equals(p.Key, root.FilePath)) ? [root.FilePath]
                : projectsOfFile.TryGetValue(root.FilePath, out HashSet<string>? found) ? found : [];
            if (!owners.Any())
            {
                return null;
            }

            foreach (string owner in owners)
            {
                kept.TryAdd(owner, $"{Where(root)} cannot be migrated");
            }
        }

        HashSet<string> withSpecFlow = new(projects.Where(p => p.Any(SpecFlowFix.ReferencesSpecFlow)).Select(p => p.Key), s_pathComparer);
        // (user, referenced SpecFlow project), directly or through other projects.
        List<(string User, string Referenced)> links =
        [
            .. projects.Where(p => withSpecFlow.Contains(p.Key))
                .SelectMany(p => References(solution, p).Where(r => withSpecFlow.Contains(r) && !s_pathComparer.Equals(r, p.Key)).Select(r => (p.Key, r))),
        ];

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach ((string path, HashSet<string> owners) in projectsOfFile.Where(f => f.Value.Count > 1 && f.Value.Any(kept.ContainsKey)))
            {
                string keeper = owners.First(kept.ContainsKey);
                foreach (string owner in owners.Where(o => !kept.ContainsKey(o)))
                {
                    kept[owner] = $"it shares {Path.GetFileName(path)} with {Path.GetFileName(keeper)}, which stays on SpecFlow";
                    changed = true;
                }
            }

            foreach ((string user, string referenced) in links.Where(l => kept.ContainsKey(l.User) != kept.ContainsKey(l.Referenced)))
            {
                if (kept.ContainsKey(user))
                {
                    kept[referenced] = $"{Path.GetFileName(user)} references it and stays on SpecFlow (its bindings must run on the same library)";
                }
                else
                {
                    kept[user] = $"it references {Path.GetFileName(referenced)}, which stays on SpecFlow (its bindings must run on the same library)";
                }

                changed = true;
            }
        }

        if (withSpecFlow.All(kept.ContainsKey))
        {
            return null;
        }

        Solution planned = solution;
        foreach (Document document in projects.Where(p => kept.ContainsKey(p.Key)).SelectMany(p => p).SelectMany(p => p.Documents))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            planned = planned.WithDocumentSyntaxRoot(document.Id, root.WithAdditionalAnnotations(new SyntaxAnnotation(PartialPlan.KeptAnnotationKind)));
        }

        // Project-file roots (G4 blockers) come back from the run itself; the plan does not seed them twice.
        List<Finding> rows = [.. roots.Where(r => r.PatternId != Fixer.ProjectFilePatternId)];
        foreach ((string project, string why) in kept.Where(k => withSpecFlow.Contains(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            rows.Add(new Finding
            {
                FilePath = project,
                Line = 0,
                FixId = "specflow",
                PatternId = KeptPatternId,
                Result = new ManualResult($"the whole project stays on SpecFlow (no per-file mode): {why}. Migrate it by hand, or fix that and run the fix again.")
                {
                    Kept = KeptReason.SpecFlowProjectKept,
                },
            });
        }

        return new PartialPlan { Solution = planned, Findings = rows };
    }

    // The projects (by file path) that any copy of this project references, directly or through other projects.
    private static HashSet<string> References(Solution solution, IEnumerable<Project> copies)
    {
        HashSet<string> found = new(s_pathComparer);
        Stack<ProjectId> pending = new(copies.SelectMany(p => p.ProjectReferences).Select(r => r.ProjectId));
        HashSet<ProjectId> seen = [];
        while (pending.TryPop(out ProjectId? id))
        {
            if (seen.Add(id) && solution.GetProject(id) is Project project)
            {
                if (project.FilePath is string path)
                {
                    found.Add(path);
                }

                foreach (ProjectReference reference in project.ProjectReferences)
                {
                    pending.Push(reference.ProjectId);
                }
            }
        }

        return found;
    }

    private static string Where(Finding finding) => finding.Line > 0 ? $"{Path.GetFileName(finding.FilePath)}:{finding.Line}" : Path.GetFileName(finding.FilePath);
}
