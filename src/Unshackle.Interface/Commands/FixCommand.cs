using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Interface.Output;

namespace Unshackle.Interface.Commands;

public class FixCommand
{
    private readonly ScanCommand _scan;
    private readonly IProjectFileStore _projectFiles;
    private readonly ISourceControl _sourceControl;
    private readonly IConsoleView _view;

    public FixCommand(ScanCommand scan, IProjectFileStore projectFiles, ISourceControl sourceControl, IConsoleView view)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(projectFiles);
        ArgumentNullException.ThrowIfNull(sourceControl);
        ArgumentNullException.ThrowIfNull(view);
        _scan = scan;
        _projectFiles = projectFiles;
        _sourceControl = sourceControl;
        _view = view;
    }

    public Task<int> ExecuteAsync(
        string solutionPath,
        string fixId,
        string? reportPath,
        bool dryRun,
        bool force,
        CancellationToken cancellationToken) =>
        ExecuteAsync(solutionPath, new FixRequest { FixId = fixId }, reportPath, dryRun, force, cancellationToken);

    public async Task<int> ExecuteAsync(
        string solutionPath,
        FixRequest request,
        string? reportPath,
        bool dryRun,
        bool force,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        try
        {
            // Only a clean working tree lets the user undo the rewrite; a dry run writes nothing.
            string directory = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!;
            if (!dryRun && !force && await HasUncommittedChangesAsync(directory, cancellationToken))
            {
                throw new CommandException(
                    ExitCodes.Failure,
                    "The working tree has uncommitted changes. Commit or stash them first, preview with --dry-run, or use --force.");
            }

            ScanResult result = await _scan.RunAsync(solutionPath, request, reportPath, force, cancellationToken);
            List<DocumentId> changed =
            [
                .. result.Report.Solution.GetChanges(result.Original).GetProjectChanges().SelectMany(p => p.GetChangedDocuments())
                    // A multi-target project has one document per target framework for the same file.
                    .DistinctBy(id => result.Report.Solution.GetDocument(id)!.FilePath ?? id.Id.ToString()),
            ];
            IReadOnlyList<ProjectFileChange> projectFileChanges = result.Report.ProjectFileChanges;
            int fileCount = changed.Count + projectFileChanges.Count;

            if (dryRun)
            {
                foreach (DocumentId id in changed)
                {
                    Document before = result.Original.GetDocument(id)!;
                    Document after = result.Report.Solution.GetDocument(id)!;
                    string oldText = (await before.GetTextAsync(cancellationToken)).ToString();
                    string newText = (await after.GetTextAsync(cancellationToken)).ToString();
                    _view.ShowDiff(before.FilePath ?? before.Name, LineDiff.Compute(oldText, newText));
                }

                foreach (ProjectFileChange change in projectFileChanges)
                {
                    // A deleted file (the old name of a renamed config file) is one line, not a diff of every line removed (TAAK-320).
                    if (change.Deletes)
                    {
                        _view.ShowMessage($"--- {change.FilePath} (deleted)");
                    }
                    else
                    {
                        _view.ShowDiff(change.FilePath, LineDiff.Compute(change.OldText, change.NewText));
                    }
                }

                _view.ShowMessage($"Dry run: {fileCount} file(s) would change, nothing written.");
                return ExitCodes.Ok;
            }

            // One save for source and project files: every file is checked before the first write.
            List<ProjectFileChange> changes = [];
            foreach (DocumentId id in changed)
            {
                Document before = result.Original.GetDocument(id)!;
                changes.Add(new ProjectFileChange
                {
                    FilePath = before.FilePath
                        ?? throw new CommandException(ExitCodes.Failure, $"{before.Name} has no file on disk; nothing written."),
                    OldText = (await before.GetTextAsync(cancellationToken)).ToString(),
                    NewText = (await result.Report.Solution.GetDocument(id)!.GetTextAsync(cancellationToken)).ToString(),
                });
            }

            try
            {
                await _projectFiles.SaveAsync([.. changes, .. projectFileChanges], cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException)
            {
                throw new CommandException(ExitCodes.Failure, $"Nothing written: {ex.Message}");
            }

            _view.ShowMessage($"{fileCount} file(s) written.");
            return ExitCodes.Ok;
        }
        catch (CommandException ex)
        {
            _view.ShowError(ex.Message);
            return ex.ExitCode;
        }
    }

    private async Task<bool> HasUncommittedChangesAsync(string directory, CancellationToken cancellationToken)
    {
        try
        {
            return await _sourceControl.HasUncommittedChangesAsync(directory, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new CommandException(ExitCodes.Failure, $"{ex.Message} Preview with --dry-run, or use --force to write anyway.");
        }
    }
}
