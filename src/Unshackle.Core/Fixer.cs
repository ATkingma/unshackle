using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

public class Fixer : IFixer
{
    /// <summary>Start of the reason of a finding that is not rewritten because other findings are Manual.</summary>
    public const string NotRewrittenPrefix = "Not rewritten: ";

    // Rows for project files have no pattern; this id groups them in the summary and the report.
    public const string ProjectFilePatternId = "project-file";

    // Rows for usings the fix could not swap safely: a global using, or a using that inactive #if code may need.
    public const string UsingsPatternId = "usings";

    // Rows for code that still names the removed package while no pattern found it: the place the user has to migrate by hand.
    public const string UnmatchedPatternId = "unmatched-use";

    // Rows for a source file that is neither UTF-8 nor Latin-1 (e.g. UTF-16): writing it back as UTF-8 would corrupt it, so it is left alone.
    // A non-UTF-8 single-byte file is loaded as Latin-1 and written back byte-exact, so it is migrated like any other (TAAK-285).
    public const string EncodingPatternId = "encoding";

    internal const string KeptPackageReason = " is still used in this project, so its package and global usings stay. ";

    internal const string KeptSharedPackageReason = " is not migrated but still uses ";

    internal const string KeptGlobalUsingReason = "The project still uses ";

    // Start of the usings row for old code in an inactive #if branch; a later pass over another target framework replaces it (TAAK-223).
    internal const string InactiveBranchReason = "Code in an inactive #if branch is not migrated";

    private static readonly IReadOnlyDictionary<string, string> s_noneBlocked = new Dictionary<string, string>();

    private readonly IReadOnlyList<IFix> _fixes;
    private readonly IProjectFileStore? _projectFiles;

    // Code only: project files are left alone.
    public Fixer(IEnumerable<IFix> fixes)
    {
        ArgumentNullException.ThrowIfNull(fixes);
        _fixes = [.. fixes];
    }

    public Fixer(IEnumerable<IFix> fixes, IProjectFileStore projectFiles)
        : this(fixes)
    {
        ArgumentNullException.ThrowIfNull(projectFiles);
        _projectFiles = projectFiles;
    }

    public async Task<FixReport> FixAsync(Solution solution, FixRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(request);
        IFix fix = _fixes.FirstOrDefault(f => string.Equals(f.Id, request.FixId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown fix '{request.FixId}'.", nameof(request));

        FixReport full = await RunAsync(fix, solution, [], cancellationToken);
        if (!fix.AllOrNothing || !full.Findings.Any(f => f.Result is ManualResult && !fix.AllowsPartial(f)))
        {
            return full;
        }

        // TAAK-274: the code tied to the Manual findings stays on the old library, the rest moves; otherwise nothing changes.
        if (fix is IPartialFix partial && await PartialRun.RunPartialAsync(partial, solution, full, (s, seed, ct) => RunAsync(fix, s, seed, ct), cancellationToken) is FixReport partialReport)
        {
            return partialReport;
        }

        return new FixReport { Solution = solution, Findings = [.. full.Findings.Select(f => f.Result is FixedResult ? FindingFactory.NotApplied(f) : f)], ProjectFileChanges = [] };
    }

    // One run of the fix over the solution; seed findings come first in the report (the rows of a partial plan).
    private async Task<FixReport> RunAsync(IFix fix, Solution solution, IReadOnlyList<Finding> seed, CancellationToken cancellationToken)
    {
        var findings = new List<Finding>(seed);
        Solution original = solution;
        Solution baseline = FileFixer.AddReferences(original, fix.AddedReferences, fix.AddedNamespaces);
        HashSet<string> swappedTypes = [.. fix.SwappedTypes];
        // A file that is not UTF-8 stays as it is, with one Manual row (TAAK-167). It still names the old library, so its
        // project keeps the package (the project-file cascade below) and the rest of the solution is migrated.
        HashSet<string> notUtf8 = await SolutionChecks.NotUtf8FilesAsync(solution, cancellationToken);
        // A file shared with a project that can reach neither the old nor the new library is not migrated either (below).
        HashSet<string> unreachable = await SolutionChecks.UnreachableFilesAsync(solution, fix, cancellationToken);
        notUtf8.UnionWith(unreachable);
        // Only a file that names the old library needs the row; another one is left alone without noise.
        foreach (string path in notUtf8.Order(StringComparer.Ordinal))
        {
            Document document = solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents).First(d => d.FilePath == path);
            if (await UsingDirectives.NamesRemovedNamespaceAsync(document, fix.RemovedNamespaces, cancellationToken))
            {
                findings.Add(FindingFactory.CreateFinding(path, 0, fix, EncodingPatternId, unreachable.Contains(path) ? PatternResult.Manual(
                    "The file is also compiled by a project that references neither the old nor the new library (a linked file, e.g. for another " +
                    "assertion library), so a rewrite would not build there. It is not migrated and its other projects keep the old package. " +
                    "Migrate it by hand.") : PatternResult.Manual(
                    "The file is not UTF-8 (it would be corrupted when written back), so it is not migrated and its project keeps the old package. " +
                    "Convert it to UTF-8 and run the fix again, or migrate it by hand.")));
            }
        }

        // A multi-target project is loaded once per target framework: the same file is fixed and reported once,
        // and its copies in the other projects get the fixed text.
        List<IGrouping<string, DocumentId>> files =
        [
            .. solution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                .SelectMany(p => p.Documents)
                .Where(d => !notUtf8.Contains(d.FilePath ?? d.Id.Id.ToString()))
                .GroupBy(d => d.FilePath ?? d.Id.Id.ToString(), d => d.Id, StringComparer.Ordinal),
        ];
        HashSet<ProjectId> globalProjects = [];
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents)
            .Where(d => !notUtf8.Contains(d.FilePath ?? d.Id.Id.ToString())))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            if (UsingDirectives.RemovedGlobalUsings(root, document.FilePath, fix.RemovedNamespaces).Any())
            {
                globalProjects.Add(document.Project.Id);
            }
        }

        foreach (IGrouping<string, DocumentId> file in files)
        {
            // TAAK-223: a copy per distinct set of active #if branches. The first pass fixes what is active in the first copy;
            // each later pass takes the text so far and fixes only the code that none of the earlier copies saw (`#if !NETFRAMEWORK`).
            SourceText? text = null;
            List<ParseOptions> earlier = [];
            foreach (DocumentId documentId in await PassCopiesAsync(solution, file, cancellationToken))
            {
                Solution passBaseline = text is null ? baseline : baseline.WithDocumentText(documentId, text);
                (Solution working, bool usingsUpdated, int firstFinding) = await FixCopyAsync(
                    fix, original, passBaseline, documentId, swappedTypes, globalProjects, earlier, findings, text, cancellationToken);
                if (earlier.Count > 0 && usingsUpdated)
                {
                    // The usings of this pass replace the inactive-branch row of an earlier one: that code may be migrated now.
                    for (int i = firstFinding - 1; i >= 0; i--)
                    {
                        if (findings[i].PatternId == UsingsPatternId && findings[i].FilePath == file.Key
                            && findings[i].Result is ManualResult { Reason: var reason } && reason.StartsWith(InactiveBranchReason, StringComparison.Ordinal))
                        {
                            findings.RemoveAt(i);
                        }
                    }
                }

                if (working != passBaseline)
                {
                    text = await working.GetDocument(documentId)!.GetTextAsync(cancellationToken);
                }

                earlier.Add(solution.GetDocument(documentId)!.Project.ParseOptions!);
            }

            if (text is not null)
            {
                foreach (DocumentId copy in file)
                {
                    solution = solution.WithDocumentText(copy, text);
                }
            }
        }

        solution = await SolutionChecks.SwapGlobalUsingsAsync(solution, fix, globalProjects, swappedTypes, notUtf8, findings, cancellationToken);
        // A package that builds on the old library: its source goes in, the package goes out with the others (TAAK-194).
        VendoredPlan vendored = await VendoredPackages.PlanAsync(original, fix, cancellationToken);
        findings.AddRange(vendored.Findings);
        solution = vendored.AddTo(solution);
        IReadOnlyList<ProjectFileChange> projectFileChanges = [];
        if (_projectFiles is not null && fix.Package is PackageMigration package)
        {
            (solution, projectFileChanges) = await MigrateProjectFilesAsync(
                _projectFiles, original, solution, fix, package, swappedTypes, vendored.Migrations, findings, cancellationToken);
        }

        projectFileChanges = [.. vendored.Files, .. projectFileChanges];
        return new FixReport { Solution = solution, Findings = findings, ProjectFileChanges = projectFileChanges };
    }

    // One copy of a file: the patterns and usings, then the checks for an added namespace that conflicts with an own extension.
    // Returns the solution with the fixed copy (passBaseline when it stays as it was), whether the usings were updated, and
    // where the findings of this copy start.
    private static async Task<(Solution Working, bool UsingsUpdated, int FirstFinding)> FixCopyAsync(
        IFix fix,
        Solution original,
        Solution passBaseline,
        DocumentId documentId,
        HashSet<string> swappedTypes,
        HashSet<ProjectId> globalProjects,
        IReadOnlyList<ParseOptions> earlier,
        List<Finding> findings,
        SourceText? text,
        CancellationToken cancellationToken)
    {
        int firstFinding = findings.Count;
        FilePass pass = await FileFixer.FixFileAsync(fix, original, passBaseline, documentId, swappedTypes, globalProjects, s_noneBlocked, false, earlier, findings, cancellationToken);
        Solution working = pass.Working;
        if (pass.Updated is not Document updated)
        {
            return (working, false, firstFinding);
        }

        // An added namespace that makes a call ambiguous with an extension of the user's own (CS0121) would break the build.
        (string Reason, int Line)? conflict = null;
        if (pass.ImportsAdded
            && await UsingDirectives.AmbiguityWithAddedAsync(updated, fix.AddedNamespaces, cancellationToken) is { } ambiguity)
        {
            conflict = ($"{ambiguity.Added.ContainingType.ToDisplayString()}.{ambiguity.Added.Name} is ambiguous with "
                + $"{ambiguity.Existing.ContainingType.ToDisplayString()}.{ambiguity.Existing.Name} once {ambiguity.Added.ContainingNamespace.ToDisplayString()} is imported (CS0121), "
                + "so this file is not migrated and its project keeps the old package. Rename or remove the conflicting extension, or migrate the file by hand.",
                ambiguity.Line);
        }
        else if (pass.Changed
            && await UsingDirectives.MisboundAddedCallAsync(passBaseline.GetDocument(documentId)!, updated, fix.AddedNamespaces, cancellationToken) is { } misbound)
        {
            // An own extension in the enclosing namespace wins without ambiguity: the rewrite would silently change behaviour (TAAK-199).
            conflict = ($"a rewritten {misbound.Bound.Name}() binds to {misbound.Bound.ContainingType.ToDisplayString()}.{misbound.Bound.Name} instead of "
                + $"{string.Join(", ", fix.AddedNamespaces)} (an own method in the enclosing namespace or type wins over an imported extension), "
                + "so this file is not migrated and its project keeps the old package. Rename or remove the conflicting method, or migrate the file by hand.",
                misbound.Line);
        }

        if (conflict is not { } c)
        {
            return (working, true, firstFinding);
        }

        // First try to migrate the rest of the file: only the classes that do not conflict are imported, and the calls
        // that would conflict stay as they were (TAAK-198). When that does not work out, the whole file stays (TAAK-174).
        List<Finding> firstPass = findings[firstFinding..];
        Solution? targeted = null;
        if (!globalProjects.Contains(documentId.ProjectId))
        {
            // References without the global using, so a check sees the imports the file itself has.
            Solution unimported = FileFixer.AddReferences(original, fix.AddedReferences, []);
            unimported = text is null ? unimported : unimported.WithDocumentText(documentId, text);
            targeted = await FileFixer.FixWithTargetedImportsAsync(
                fix, original, passBaseline, unimported, documentId, swappedTypes, globalProjects, earlier, findings, firstFinding, cancellationToken);
        }

        if (targeted is not null)
        {
            return (targeted, true, firstFinding);
        }

        findings.RemoveRange(firstFinding, findings.Count - firstFinding);
        findings.AddRange(firstPass);
        for (int i = firstFinding; i < findings.Count; i++)
        {
            if (findings[i].Result is FixedResult)
            {
                findings[i] = FindingFactory.CreateFinding(findings[i], PatternResult.Manual($"{NotRewrittenPrefix}{c.Reason}"));
            }
        }

        findings.Add(FindingFactory.CreateUsingsFinding(updated.FilePath ?? updated.Name, c.Line, fix, PatternResult.Manual(c.Reason)));
        return (passBaseline, false, firstFinding);
    }

    // TAAK-223: the copies of a file (one per target framework) that each need a pass: one per distinct set of inactive #if code.
    // A file without directives needs only its first copy.
    private static async Task<IReadOnlyList<DocumentId>> PassCopiesAsync(Solution solution, IEnumerable<DocumentId> copies, CancellationToken cancellationToken)
    {
        List<DocumentId> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (DocumentId copy in copies)
        {
            SyntaxNode root = (await solution.GetDocument(copy)!.GetSyntaxRootAsync(cancellationToken))!;
            if (!root.ContainsDirectives && result.Count > 0)
            {
                break;
            }

            string signature = string.Join(";", root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)).Select(t => t.FullSpan.ToString()));
            if (seen.Add(signature))
            {
                result.Add(copy);
            }
        }

        return result;
    }

    // Runs on the fixed solution: only code that is still there can keep the removed package.
    // When the project files cannot be edited, the code of those projects stays as it was: rewritten code without the new
    // package would not build (TAAK-111).
    private static async Task<(Solution Solution, IReadOnlyList<ProjectFileChange> Changes)> MigrateProjectFilesAsync(
        IProjectFileStore projectFiles,
        Solution original,
        Solution solution,
        IFix fix,
        PackageMigration package,
        HashSet<string> swappedTypes,
        IReadOnlyList<ProjectMigration> vendoredMigrations,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        // One edit for both: the store reads each file once, so the second swap does not overwrite the first.
        IReadOnlyList<ProjectMigration> migrations =
            [.. await ProjectPackages.PlanAsync(original, solution, fix, package, swappedTypes, cancellationToken), .. vendoredMigrations];
        if (migrations.Count == 0)
        {
            return (solution, []);
        }

        IReadOnlyList<ProjectFileChange> changes;
        try
        {
            changes = await projectFiles.EditAsync(migrations, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Like a pattern: a broken project file is manual work, not a lost run.
            string reason = $"Could not edit the project files: {ex.Message}";
            HashSet<string> projectPaths = [.. migrations.Select(m => m.ProjectPath)];
            List<Document> documents = [.. solution.Projects.Where(p => p.FilePath is not null && projectPaths.Contains(p.FilePath)).SelectMany(p => p.Documents)];
            HashSet<string> filePaths = [.. documents.Select(d => d.FilePath ?? d.Name)];
            foreach (Document document in documents)
            {
                solution = solution.WithDocumentText(document.Id, await original.GetDocument(document.Id)!.GetTextAsync(cancellationToken));
            }

            for (int i = 0; i < findings.Count; i++)
            {
                if (findings[i].Result is FixedResult && filePaths.Contains(findings[i].FilePath))
                {
                    findings[i] = FindingFactory.CreateFinding(findings[i], PatternResult.Manual($"{NotRewrittenPrefix}{reason}"));
                }
            }

            findings.AddRange(migrations.Select(m => FindingFactory.CreateProjectFinding(m.ProjectPath, fix, PatternResult.Manual(reason))));
            return (solution, []);
        }

        findings.AddRange(migrations.Where(m => m.KeepRemovedPackage).Select(m => FindingFactory.CreateProjectFinding(m.ProjectPath, fix,
            PatternResult.Manual($"{package.RemovedPackage}{KeptPackageReason}Fix the manual findings and run the fix again."))));
        // The project-file row alone says that something is left, not where. A file with a Manual finding already has its explanation.
        HashSet<string> removed = [package.RemovedPackage, .. package.AlsoRemovedPackages];
        HashSet<string> explained = [.. findings.Where(f => f.Result is ManualResult).Select(f => f.FilePath)];
        foreach (ProjectMigration kept in migrations.Where(m => m.KeepRemovedPackage))
        {
            foreach ((string filePath, int line, string symbol) in await ProjectPackages.RemainingUsesAsync(solution, kept.ProjectPath, removed, swappedTypes, cancellationToken))
            {
                if (!explained.Contains(filePath))
                {
                    findings.Add(FindingFactory.CreateFinding(filePath, line, fix, UnmatchedPatternId, PatternResult.Manual(
                        $"{symbol} is still used here and no pattern migrates it, so {package.RemovedPackage} stays in this project. Migrate it by hand.")));
                }
            }
        }

        findings.AddRange(changes.Select(c => FindingFactory.CreateProjectFinding(c.FilePath, fix, c.KeptRemovedPackageFor is string user
            ? PatternResult.Manual($"{user}{KeptSharedPackageReason}{package.RemovedPackage}, so this shared file keeps it next to the new package. " +
                "Migrate that project and run the fix again.")
            : PatternResult.Fixed(Array.Empty<SyntaxNode>()))));
        return (solution, changes);
    }
}
