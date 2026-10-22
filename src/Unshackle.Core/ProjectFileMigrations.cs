using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// TAAK-317, council round 8 S3: the typed project/config-file edits of a fix, after the document pass. A project gets them only when its
// code has no blocking Manual row (a kept finding is planned, not blocking); each edit is one row with file:line.
internal static class ProjectFileMigrations
{
    internal const string BlockedReason = "Project-file edits not applied: this project's code still has manual findings. Fix them and run the fix again.";

    internal const string WarnedPropertyReason = " changes how the project builds or runs (an executable or test host); check that this is what you want.";

    private static readonly StringComparer s_pathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

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
        foreach (IGrouping<string, Project> project in original.Projects
            .Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null)
            .GroupBy(p => p.FilePath!, s_pathComparer))
        {
            IReadOnlyList<ProjectFileEdit> edits = migration.EditsFor(project.First());
            if (edits.Count == 0)
            {
                continue;
            }

            HashSet<string> documents = new(project.SelectMany(p => p.Documents).Select(d => d.FilePath ?? d.Name), s_pathComparer);
            int blocking = findings.Count(f => documents.Contains(f.FilePath) && f.Result is ManualResult { Kept: KeptReason.None });
            if (blocking > 0)
            {
                findings.Add(FindingFactory.CreateProjectFinding(project.Key, fix, PatternResult.Manual($"{BlockedReason} ({blocking} finding(s))")));
                continue;
            }

            IReadOnlyList<ProjectFileEditOutcome> outcomes;
            try
            {
                outcomes = await editor.ApplyAsync(project.Key, edits, changes, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Like a pattern: a broken file is manual work, not a lost run. The project's code is migrated already and builds without these edits.
                findings.Add(FindingFactory.CreateProjectFinding(project.Key, fix, PatternResult.Manual($"Could not edit the project files: {ex.Message}")));
                continue;
            }

            foreach (ProjectFileEditOutcome outcome in outcomes)
            {
                findings.Add(FindingFactory.CreateFinding(outcome.FilePath, outcome.Line, fix, Fixer.ProjectFilePatternId, Result(outcome)));
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

    private static PatternResult Result(ProjectFileEditOutcome outcome)
    {
        if (outcome.ManualReason is string reason)
        {
            return PatternResult.Manual($"{outcome.Edit.Describe()}: {reason}");
        }

        return new FixedResult([], [])
        {
            Info = outcome.Edit.Describe(),
            Warning = outcome.Edit is PropertyEdit property && s_warnedProperties.Contains(property.Name) ? property.Name + WarnedPropertyReason : null,
        };
    }
}
