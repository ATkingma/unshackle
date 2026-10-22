using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// TAAK-317, council round 8 S3: the typed project/config-file edits of a fix, after the document pass. A project gets them only when it has
// no blocking row (council round 10, G4): a blocking Manual row in its code (a kept finding is planned, not blocking) or a project-level row
// with a ProjectBlockingReason code. Each edit is one row with file:line.
internal static class ProjectFileMigrations
{
    internal const string BlockedReason = "Project-file edits not applied: this project's code still has manual findings. Fix them and run the fix again.";

    internal const string BlockedByProjectReason = "Project-file edits not applied: ";

    internal const string WarnedPropertyReason = " changes how the project builds or runs (an executable or test host); check that this is what you want.";

    private static readonly StringComparer s_pathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static readonly HashSet<string> s_blockingCodes = [.. Enum.GetNames<ProjectBlockingReason>()];

    // MSBuild properties whose edit always warns (council round 8, S3).
    private static readonly HashSet<string> s_warnedProperties = new(["OutputType", "IsTestProject"], StringComparer.OrdinalIgnoreCase);

    internal static async Task<IReadOnlyList<ProjectFileChange>> ApplyAsync(
        IFix fix,
        IProjectFileMigration migration,
        IProjectFileEditor editor,
        Solution original,
        List<Finding> findings,
        IReadOnlyList<ProjectFileChange> earlier,
        CancellationToken cancellationToken)
    {
        List<ProjectFileChange> changes = [.. earlier];
        // A multi-target project is loaded once per target framework: its file is edited once, with the documents of every copy.
        List<IGrouping<string, Project>> projects =
        [
            .. original.Projects.Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null).GroupBy(p => p.FilePath!, s_pathComparer),
        ];
        // The ProjectBlockers rows are in the findings already: the Fixer adds them before the document pass (TAAK-320).
        // Council round 10, G3/G4: every project's blocking rows are known before any shared file is edited.
        var blocked = new Dictionary<string, string>(s_pathComparer);
        foreach (IGrouping<string, Project> project in projects)
        {
            if (Blocker(project, findings) is string why)
            {
                blocked[project.Key] = why;
            }
        }

        var context = new ProjectFileEditContext { LoadedProjects = new HashSet<string>(projects.Select(p => p.Key), s_pathComparer), BlockedProjects = blocked };
        foreach (IGrouping<string, Project> project in projects)
        {
            IReadOnlyList<ProjectFileEdit> edits = migration.EditsFor(project.First());
            if (edits.Count == 0)
            {
                continue;
            }

            if (blocked.TryGetValue(project.Key, out string? why))
            {
                findings.Add(FindingFactory.CreateProjectFinding(project.Key, fix, PatternResult.Manual(why)));
                continue;
            }

            IReadOnlyList<ProjectFileEditOutcome> outcomes;
            try
            {
                outcomes = await editor.ApplyAsync(project.Key, edits, changes, context, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Like a pattern: a broken file is manual work, not a lost run. The project's code is migrated already and builds without these edits.
                findings.Add(FindingFactory.CreateProjectFinding(project.Key, fix, PatternResult.Manual($"Could not edit the project files: {ex.Message}")));
                continue;
            }

            // G4: an edit that turns out blocked (a shared declaration) blocks the project's other edits too.
            ProjectFileEditOutcome? blocking = outcomes.FirstOrDefault(o => o.ManualCode is string code && s_blockingCodes.Contains(code));
            foreach (ProjectFileEditOutcome outcome in outcomes)
            {
                PatternResult result = blocking is null || outcome.ManualReason is not null
                    ? Result(outcome)
                    : PatternResult.Manual($"{BlockedByProjectReason}{outcome.Edit.Describe()} waits for {blocking.Edit.Describe()}.");
                findings.Add(FindingFactory.CreateFinding(outcome.FilePath, outcome.Line, fix, Fixer.ProjectFilePatternId, result));
                if (blocking is not null)
                {
                    continue;
                }

                foreach (ProjectFileChange change in outcome.Changes)
                {
                    int index = changes.FindIndex(c => s_pathComparer.Equals(c.FilePath, change.FilePath));
                    if (index >= 0)
                    {
                        changes[index] = change;
                    }
                    else
                    {
                        changes.Add(change);
                    }
                }
            }
        }

        return changes;
    }

    // TAAK-320: the ProjectBlockers rows of every project file, and per document file of a project with a blocking code (G4) the text
    // "<code> on <project>". Those documents are not rewritten, so no fix half-migrates a blocked project. TAAK-321: per blocked project
    // file its code, so the package migration leaves that project alone.
    internal static (List<Finding> Rows, Dictionary<string, string> BlockedFiles, Dictionary<string, string> BlockedProjects) Blockers(
        IFix fix, IProjectFileMigration migration, Solution original)
    {
        List<Finding> rows = [];
        var blockedFiles = new Dictionary<string, string>(s_pathComparer);
        var blockedProjects = new Dictionary<string, string>(s_pathComparer);
        foreach (IGrouping<string, Project> project in original.Projects
            .Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null).GroupBy(p => p.FilePath!, s_pathComparer))
        {
            IReadOnlyList<ManualResult> blockers = migration.ProjectBlockers(project.First());
            rows.AddRange(blockers.Select(b => FindingFactory.CreateProjectFinding(project.Key, fix, b)));
            if (blockers.FirstOrDefault(b => b.Code is string code && s_blockingCodes.Contains(code)) is { Code: string blocking })
            {
                blockedProjects[project.Key] = blocking;
                foreach (string file in project.SelectMany(p => p.Documents).Select(d => d.FilePath ?? d.Name))
                {
                    blockedFiles.TryAdd(file, $"{blocking} on {project.Key}");
                }
            }
        }

        return (rows, blockedFiles, blockedProjects);
    }

    // Why the project's file edits are blocked (G4), or null: blocking Manual rows in its documents, or a project-level row with a blocking code.
    private static string? Blocker(IGrouping<string, Project> project, List<Finding> findings)
    {
        HashSet<string> documents = new(project.SelectMany(p => p.Documents).Select(d => d.FilePath ?? d.Name), s_pathComparer);
        int blocking = findings.Count(f => documents.Contains(f.FilePath) && f.Result is ManualResult { Kept: KeptReason.None });
        if (blocking > 0)
        {
            return $"{BlockedReason} ({blocking} finding(s))";
        }

        return findings.FirstOrDefault(f => s_pathComparer.Equals(f.FilePath, project.Key) && f.Result is ManualResult { Code: string code } && s_blockingCodes.Contains(code))
            is { Result: ManualResult manual }
            ? $"{BlockedByProjectReason}{KeptReasonText.Rendered(manual)}"
            : null;
    }

    private static PatternResult Result(ProjectFileEditOutcome outcome)
    {
        if (outcome.ManualReason is string reason)
        {
            return new ManualResult($"{outcome.Edit.Describe()}: {reason}") { Code = outcome.ManualCode };
        }

        if (outcome.KeptInfo is string kept)
        {
            return PatternResult.Note(kept);
        }

        return new FixedResult([], [])
        {
            Info = outcome.Note is null ? outcome.Edit.Describe() : $"{outcome.Edit.Describe()}. {outcome.Note}",
            Warning = outcome.Edit is PropertyEdit property && s_warnedProperties.Contains(property.Name) ? property.Name + WarnedPropertyReason : null,
        };
    }
}
