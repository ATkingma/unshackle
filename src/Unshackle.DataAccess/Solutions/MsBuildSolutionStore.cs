using System.Globalization;
using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Unshackle.Core.Abstractions;

namespace Unshackle.DataAccess.Solutions;

// Only loads: FixCommand writes the changed documents through IProjectFileStore, so every file gets the same
// staleness check before the first write (Workspace.TryApplyChanges would overwrite edits made after the load).
public class MsBuildSolutionStore : ISolutionStore, IDisposable
{
    private readonly IFileSystem _fileSystem;
    private MSBuildWorkspace? _workspace;

    public MsBuildSolutionStore(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public async Task<Solution> LoadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = _fileSystem.Path.GetFullPath(path);
        if (!_fileSystem.File.Exists(fullPath))
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

        if (errors.Count > 0)
        {
            throw new SolutionLoadException(errors);
        }

        // An unrestored solution loads without a failure, but package types do not bind: the fix would find nothing and
        // still swap usings and packages blindly. A missing namespace or type is the symptom we can see.
        foreach (Project project in solution.Projects)
        {
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
            errors.AddRange(compilation?.GetDiagnostics(cancellationToken)
                .Where(d => d.Id is "CS0246" or "CS0234" && d.Severity == DiagnosticSeverity.Error)
                .Take(3)
                .Select(d => $"{project.Name}: {d.GetMessage(CultureInfo.InvariantCulture)}") ?? []);
        }

        return errors.Count > 0
            ? throw new SolutionLoadException(
                [.. errors, $"Types do not resolve. Run 'dotnet restore \"{fullPath}\"' and make sure the code builds first."])
            : solution;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
        GC.SuppressFinalize(this);
    }
}
