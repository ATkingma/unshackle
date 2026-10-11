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
    // Global properties for the design-time build only; the user's files stay untouched. A vulnerable or net4x package
    // (NuGet audit, NU1701) is a build concern, not an analysis one, but with TreatWarningsAsErrors it fails the load.
    private static readonly Dictionary<string, string> s_loadProperties = new()
    {
        ["NuGetAudit"] = "false",
        ["WarningsNotAsErrors"] = "NU1701;NU1900;NU1901;NU1902;NU1903;NU1904;NU1905",
    };

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
        _workspace = MSBuildWorkspace.Create(s_loadProperties);
        var failures = new List<string>();
        _workspace.RegisterWorkspaceFailedHandler(e =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                failures.Add(e.Diagnostic.Message);
            }
        });

        bool isProject = Path.GetExtension(fullPath).EndsWith("proj", StringComparison.OrdinalIgnoreCase);
        Solution solution = isProject
            ? (await _workspace.OpenProjectAsync(fullPath, cancellationToken: cancellationToken)).Solution
            : await _workspace.OpenSolutionAsync(fullPath, cancellationToken: cancellationToken);

        var errors = new List<string>();
        var warnings = new List<string>();
        foreach (string failure in failures)
        {
            if (IsTolerable(failure, solution))
            {
                warnings.Add(failure);
            }
            else
            {
                errors.Add(failure);
            }
        }

        Warnings = warnings;
        if (errors.Count > 0)
        {
            throw new SolutionLoadException([.. errors, .. warnings]);
        }

        // A project the solution restore skipped (Bogus' NUKE 'Builder', TAAK-112) resolves not even System, while the others do.
        // Nothing binds in it, so the fix could only damage it: with no project depending on it, it is left out with a warning.
        // A lone unrestored project, or one others depend on, stays fatal below. A shared package file may still change for the
        // others; the left-out project does not build with the solution anyway (the restore that skipped it is the solution's).
        List<Project> unresolved = [];
        foreach (Project project in solution.Projects)
        {
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation?.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error)
            {
                unresolved.Add(project);
            }
        }

        if (unresolved.Count < solution.ProjectIds.Count)
        {
            HashSet<ProjectId> referenced = [.. solution.Projects.SelectMany(p => p.ProjectReferences).Select(r => r.ProjectId)];
            foreach (Project project in unresolved.Where(u => !referenced.Contains(u.Id)))
            {
                warnings.Add($"{project.Name}: not even System resolves (not restored with the solution?), so its code is left alone.");
                solution = solution.RemoveProject(project.Id);
            }
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
                [.. errors, .. warnings, $"Types do not resolve. Run 'dotnet restore \"{fullPath}\"' and make sure the code builds first."])
            : solution;
    }

    public IReadOnlyList<string> Warnings { get; private set; } = [];

    // Roslyn reports every MSBuild message of a project as a Failure, also plain warnings such as NuGet audit. Two cases
    // are safe to continue with: the project still loaded (the CS0246 check below catches references that did not
    // resolve), or the project file does not exist at all (nothing on disk to rewrite, and it cannot build anyway).
    // Any other failure, such as a project that exists but did not load, stays fatal: the fix would skip its code while
    // still changing shared package files.
    private bool IsTolerable(string failure, Solution solution)
    {
        IEnumerable<string> paths = failure.Split('\'').Where(p => p.EndsWith("proj", StringComparison.OrdinalIgnoreCase));
        return paths.Any(p => solution.Projects.Any(project => string.Equals(project.FilePath, p, StringComparison.Ordinal))
            || !_fileSystem.File.Exists(p));
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
        GC.SuppressFinalize(this);
    }
}
