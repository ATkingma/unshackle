using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Interface.Output;

namespace Unshackle.Interface.Commands;

// Loads, runs the fix and reports, without writing code. FixCommand reuses RunAsync.
public class ScanCommand
{
    private readonly IFixer _fixer;
    private readonly ISolutionStore _store;
    private readonly IReadOnlyList<IReportWriter> _reportWriters;
    private readonly IConsoleView _view;

    public ScanCommand(IFixer fixer, ISolutionStore store, IEnumerable<IReportWriter> reportWriters, IConsoleView view)
    {
        ArgumentNullException.ThrowIfNull(fixer);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(reportWriters);
        ArgumentNullException.ThrowIfNull(view);
        _fixer = fixer;
        _store = store;
        _reportWriters = [.. reportWriters];
        _view = view;
    }

    public Task<int> ExecuteAsync(
        string solutionPath, string fixId, string? reportPath, bool force, CancellationToken cancellationToken) =>
        ExecuteAsync(solutionPath, new FixRequest { FixId = fixId }, reportPath, force, cancellationToken);

    public async Task<int> ExecuteAsync(
        string solutionPath, FixRequest request, string? reportPath, bool force, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(solutionPath, request, reportPath, force, cancellationToken);
            return ExitCodes.Ok;
        }
        catch (CommandException ex)
        {
            _view.ShowError(ex.Message);
            return ex.ExitCode;
        }
    }

    // Throws CommandException with the exit code on any expected failure. Force lets the report replace an existing file.
    public Task<ScanResult> RunAsync(
        string solutionPath, string fixId, string? reportPath, bool force, CancellationToken cancellationToken) =>
        RunAsync(solutionPath, new FixRequest { FixId = fixId }, reportPath, force, cancellationToken);

    // The request carries how files are processed (--sequential, --max-degree); the output is the same either way.
    public async Task<ScanResult> RunAsync(
        string solutionPath, FixRequest request, string? reportPath, bool force, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FixId);
        IReportWriter? reportWriter = reportPath is null ? null : FindReportWriter(reportPath);
        // --report README.md must not silently replace a file of the user.
        if (reportPath is not null && !force && File.Exists(reportPath))
        {
            throw new CommandException(ExitCodes.Failure, $"The report file {reportPath} already exists. Use --force to overwrite it.");
        }

        Solution solution;
        try
        {
            _view.ShowMessage($"Loading {solutionPath}...");
            solution = await _store.LoadAsync(solutionPath, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            throw new CommandException(ExitCodes.Failure, ex.Message);
        }
        catch (SolutionLoadException ex)
        {
            throw new CommandException(ExitCodes.Failure, string.Join(Environment.NewLine, [ex.Message, .. ex.Errors]));
        }

        foreach (string warning in _store.Warnings)
        {
            _view.ShowMessage($"Warning: {warning}");
        }

        FixReport report;
        try
        {
            report = await _fixer.FixAsync(solution, request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw new CommandException(ExitCodes.InvalidArguments, ex.Message);
        }

        _view.ShowSummary(report.Findings);
        _view.ShowBlockingProjects(report.BlockingProjects);
        if (reportWriter is not null)
        {
            await WriteReportAsync(reportWriter, reportPath!, solutionPath, report.Findings, cancellationToken);
        }

        return new ScanResult { Original = solution, Report = report };
    }

    private IReportWriter FindReportWriter(string reportPath)
    {
        string extension = Path.GetExtension(reportPath);
        return _reportWriters.FirstOrDefault(w => string.Equals(w.Extension, extension, StringComparison.OrdinalIgnoreCase))
            ?? throw new CommandException(
                ExitCodes.InvalidArguments,
                $"Unknown report format '{extension}'. Use {string.Join(", ", _reportWriters.Select(w => w.Extension))}.");
    }

    private async Task WriteReportAsync(
        IReportWriter writer,
        string reportPath,
        string solutionPath,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken)
    {
        // Paths relative to the solution keep the report readable and the same on every machine.
        string root = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!;
        IEnumerable<Finding> relative = findings.Select(f => new Finding
        {
            FilePath = Path.GetRelativePath(root, f.FilePath).Replace('\\', '/'),
            Line = f.Line,
            FixId = f.FixId,
            PatternId = f.PatternId,
            Result = f.Result,
        });
        try
        {
            await writer.WriteAsync(reportPath, relative, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CommandException(ExitCodes.Failure, $"Could not write the report: {ex.Message}");
        }

        _view.ShowMessage($"Report written to {reportPath}");
    }
}
