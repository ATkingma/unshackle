using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
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

    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IFileSystem _fileSystem;
    private readonly HashSet<string> _libraries;
    private MSBuildWorkspace? _workspace;

    /// <param name="fileSystem">Reads project files.</param>
    /// <param name="libraryNamespaces">Namespaces of the libraries the fixes migrate (FluentAssertions, MediatR). A project whose
    /// types do not resolve is only left out when it uses none of them.</param>
    public MsBuildSolutionStore(IFileSystem fileSystem, IEnumerable<string> libraryNamespaces)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(libraryNamespaces);
        _fileSystem = fileSystem;
        _libraries = [.. libraryNamespaces.Select(n => n.Split('.')[0])];
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
        solution = await MarkNotUtf8FilesAsync(solution, cancellationToken);

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
        // TAAK-142: an AOT app, benchmark or sample (Akka.NET, Steeltoe) that does not resolve, uses none of the migrated libraries
        // and has no project depending on it, is left out with a warning, as long as another project does resolve.
        Dictionary<Project, List<string>> broken = [];
        foreach (Project project in solution.Projects)
        {
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
            List<Diagnostic> unresolvedNames = compilation?.GetDiagnostics(cancellationToken)
                .Where(d => d.Id is "CS0246" or "CS0234" && d.Severity == DiagnosticSeverity.Error).ToList() ?? [];
            HashSet<string> razorNames = unresolvedNames.Count > 0 ? RazorNames(project) : [];
            List<string> messages = [.. unresolvedNames
                .Select(d => d.GetMessage(CultureInfo.InvariantCulture))
                .Where(m => !razorNames.Contains(m.Split('\'')[1]))];
            if (messages.Count > 0)
            {
                broken[project] = messages;
            }
        }

        if (broken.Count < solution.ProjectIds.Count)
        {
            HashSet<ProjectId> referenced = [.. solution.Projects.SelectMany(p => p.ProjectReferences).Select(r => r.ProjectId)];
            foreach ((Project project, List<string> messages) in broken.Where(b => !referenced.Contains(b.Key.Id) && !UsesLibrary(b.Key, b.Value)).ToList())
            {
                warnings.Add($"{project.Name}: types do not resolve ({messages[0]}), but it uses no migrated library, so its code is left alone.");
                solution = solution.RemoveProject(project.Id);
                broken.Remove(project);
            }
        }

        errors.AddRange(broken.SelectMany(b => b.Value.Take(3).Select(m => $"{b.Key.Name}: {m}")));

        return errors.Count > 0
            ? throw new SolutionLoadException(
                [.. errors, .. warnings, $"Types do not resolve. Run 'dotnet restore \"{fullPath}\"' and make sure the code builds first."])
            : solution;
    }

    public IReadOnlyList<string> Warnings { get; private set; } = [];

    // Roslyn decodes a file that is not valid UTF-8 with the system ANSI code page. On Windows that is 1252; on Linux and macOS
    // it is UTF-8 itself, so a Latin-1 file loads as UTF-8 with replacement characters and the fixer would rewrite it (TAAK-167,
    // red CI). Such a file gets its text as Latin-1, so every platform reports it as not UTF-8. Only a text with a replacement
    // character can be one, which keeps the extra read to those files.
    private async Task<Solution> MarkNotUtf8FilesAsync(Solution solution, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.SelectMany(p => p.Documents).ToList())
        {
            SourceText text = await document.GetTextAsync(cancellationToken);
            if (document.FilePath is null || text.Encoding is not UTF8Encoding || !text.ToString().Contains('�', StringComparison.Ordinal))
            {
                continue;
            }

            byte[] bytes = await _fileSystem.File.ReadAllBytesAsync(document.FilePath, cancellationToken);
            try
            {
                _ = s_strictUtf8.GetCharCount(bytes);
            }
            catch (DecoderFallbackException)
            {
                solution = solution.WithDocumentText(document.Id, SourceText.From(bytes, bytes.Length, Encoding.Latin1));
            }
        }

        return solution;
    }

    // A migrated library is in use when the project references its assembly or names it in a name that does not resolve.
    private bool UsesLibrary(Project project, List<string> messages) =>
        project.MetadataReferences.OfType<PortableExecutableReference>()
            .Any(r => IsLibrary(_fileSystem.Path.GetFileNameWithoutExtension(r.FilePath)))
        || messages.Any(m => m.Split('\'').Skip(1).Any(IsLibrary));

    private bool IsLibrary(string? name) =>
        name is not null && _libraries.Any(l => name == l || name.StartsWith(l + ".", StringComparison.Ordinal));

    // Roslyn reports every MSBuild message of a project as a Failure, also plain warnings such as NuGet audit. Two cases
    // are safe to continue with: the project still loaded (the CS0246 check below catches references that did not
    // resolve), or the project file does not exist at all (nothing on disk to rewrite, and it cannot build anyway).
    // Any other failure, such as a project that exists but did not load, stays fatal: the fix would skip its code while
    // still changing shared package files.
    private bool IsTolerable(string failure, Solution solution)
    {
        IEnumerable<string> paths = failure.Split('\'')
            .Where(p => p.EndsWith("proj", StringComparison.OrdinalIgnoreCase) && _fileSystem.Path.IsPathRooted(p));
        return paths.Any(p => solution.Projects.Any(project => string.Equals(project.FilePath, p, StringComparison.Ordinal))
            || !_fileSystem.File.Exists(p)
            || SharesNoPackage(p, solution));
    }

    // TAAK-135: a project that does not load (.dcproj, .sqlproj, a missing SDK) can be skipped when it shares no package with
    // the loaded projects: then a shared package file the fix changes is not one it uses.
    // shortcut: only PackageReference items in the project files themselves; one added via Directory.Build.props is not seen.
    private bool SharesNoPackage(string projectPath, Solution solution)
    {
        HashSet<string> loaded = [.. solution.Projects
            .Select(p => p.FilePath).OfType<string>().Distinct()
            .SelectMany(PackageIds)];
        return !PackageIds(projectPath).Any(loaded.Contains);
    }

    private IEnumerable<string> PackageIds(string projectPath) =>
        _fileSystem.File.Exists(projectPath)
            ? Regex.Matches(_fileSystem.File.ReadAllText(projectPath), @"<PackageReference\s+(?:Include|Update)\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)
                .Select(m => m.Groups[1].Value.ToUpperInvariant())
            : [];

    // The workspace does not run the Razor generator: a component (Edit.razor) or the folder namespace of components
    // (eShop.WebApp.Components) does not resolve, while dotnet build compiles it.
    private HashSet<string> RazorNames(Project project)
    {
        string? directory = _fileSystem.Path.GetDirectoryName(project.FilePath);
        if (directory is null || !_fileSystem.Directory.Exists(directory))
        {
            return [];
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (string file in _fileSystem.Directory.EnumerateFiles(directory, "*.razor", SearchOption.AllDirectories))
        {
            names.Add(_fileSystem.Path.GetFileNameWithoutExtension(file));
            names.UnionWith(_fileSystem.Path.GetRelativePath(directory, file).Split('/', '\\').SkipLast(1));
        }

        return names;
    }

    public void Dispose()
    {
        _workspace?.Dispose();
        _workspace = null;
        GC.SuppressFinalize(this);
    }
}
