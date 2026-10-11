using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Unshackle.Core.Abstractions;

namespace Unshackle.DataAccess.Solutions;

// One workspace per store: SaveAsync applies changes to the workspace that loaded the solution.
public class MsBuildSolutionStore : ISolutionStore, IDisposable
{
    private MSBuildWorkspace? _workspace;

    public async Task<Solution> LoadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fullPath}", fullPath);
        }

        _workspace?.Dispose();
        _workspace = MSBuildWorkspace.Create();
        var errors = new List<string>();
        _workspace.RegisterWorkspaceFailedHandler(e =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                errors.Add(e.Diagnostic.Message);
            }
        });

        bool isProject = Path.GetExtension(fullPath).EndsWith("proj", StringComparison.OrdinalIgnoreCase);
        Solution solution = isProject
            ? (await _workspace.OpenProjectAsync(fullPath, cancellationToken: cancellationToken)).Solution
            : await _workspace.OpenSolutionAsync(fullPath, cancellationToken: cancellationToken);

        return errors.Count > 0 ? throw new SolutionLoadException(errors) : solution;
    }

    public Task SaveAsync(Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        cancellationToken.ThrowIfCancellationRequested();
        if (_workspace is null)
        {
            throw new InvalidOperationException("Load a solution before saving it.");
        }

        return _workspace.TryApplyChanges(solution)
            ? Task.CompletedTask
            : throw new InvalidOperationException("The changes could not be written; was the solution loaded by this store?");
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
        GC.SuppressFinalize(this);
    }
}
