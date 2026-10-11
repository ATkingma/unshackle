using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Interface.Output;

namespace Unshackle.Interface.Commands;

public class FixCommand
{
    private readonly ScanCommand _scan;
    private readonly ISolutionStore _store;
    private readonly ISourceControl _sourceControl;
    private readonly IConsoleView _view;

    public FixCommand(ScanCommand scan, ISolutionStore store, ISourceControl sourceControl, IConsoleView view)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(sourceControl);
        ArgumentNullException.ThrowIfNull(view);
        _scan = scan;
        _store = store;
        _sourceControl = sourceControl;
        _view = view;
    }

    public async Task<int> ExecuteAsync(
        string solutionPath,
        string fixId,
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
            if (!dryRun && !force && await _sourceControl.HasUncommittedChangesAsync(directory, cancellationToken))
            {
                throw new CommandException(
                    ExitCodes.Failure,
                    "The working tree has uncommitted changes. Commit or stash them first, preview with --dry-run, or use --force.");
            }

            ScanResult result = await _scan.RunAsync(solutionPath, fixId, reportPath, cancellationToken);
            List<DocumentId> changed =
            [
                .. result.Report.Solution.GetChanges(result.Original).GetProjectChanges().SelectMany(p => p.GetChangedDocuments()),
            ];

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

                _view.ShowMessage($"Dry run: {changed.Count} file(s) would change, nothing written.");
                return ExitCodes.Ok;
            }

            try
            {
                await _store.SaveAsync(result.Report.Solution, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                throw new CommandException(ExitCodes.Failure, $"Could not write the changes: {ex.Message}");
            }

            _view.ShowMessage($"{changed.Count} file(s) written.");
            return ExitCodes.Ok;
        }
        catch (CommandException ex)
        {
            _view.ShowError(ex.Message);
            return ex.ExitCode;
        }
    }
}
