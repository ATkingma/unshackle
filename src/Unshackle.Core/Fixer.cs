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

    // The header row of a report-only fix (council round 8, S2): an Info row, first in the report, that says "inventory only".
    public const string InventoryOnlyPatternId = "inventory-only";

    // The one fix-level Info row of a run that changed code (council round 14, M13).
    public const string ChangedCodeInfoPatternId = "changed-code-info";

    internal const string KeptPackageReason = " is still used in this project, so its package and global usings stay. ";

    internal const string KeptSharedPackageReason = " is not migrated but still uses ";

    // A helper project that keeps the package only because a project referencing it still uses it (corpus Moq B2).
    internal const string KeptForDependentReason = " stays in this project because a project that references it still uses it: ";

    internal const string KeptGlobalUsingReason = "The project still uses ";

    // Start of the usings row for old code in an inactive #if branch; a later pass over another target framework replaces it (TAAK-223).
    internal const string InactiveBranchReason = "Code in an inactive #if branch is not migrated";

    // Each file in flight holds its own semantic models: the cap bounds memory (council round 7, TAAK-298).
    public const int DefaultMaxDegree = 8;

    private static readonly IReadOnlyDictionary<string, string> s_noneBlocked = new Dictionary<string, string>();

    private readonly IReadOnlyList<IFix> _fixes;
    private readonly IProjectFileStore? _projectFiles;
    private readonly IPackageAssetsReader? _assetsReader;

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

    // TAAK-329: with the restored assets, a package lost with the removed one is pinned where code still uses it (council round 16, G10).
    public Fixer(IEnumerable<IFix> fixes, IProjectFileStore projectFiles, IPackageAssetsReader assetsReader)
        : this(fixes, projectFiles)
    {
        ArgumentNullException.ThrowIfNull(assetsReader);
        _assetsReader = assetsReader;
    }

    public async Task<FixReport> FixAsync(Solution solution, FixRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(request);
        IFix fix = _fixes.FirstOrDefault(f => string.Equals(f.Id, request.FixId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown fix '{request.FixId}'.", nameof(request));

        // Council round 8, S2: a report-only fix takes inventory; nothing in the solution or its project files changes.
        if (fix is IReportOnlyFix reportOnly)
        {
            return await ReportOnlyRun.RunAsync(fix, reportOnly, solution, request, cancellationToken);
        }

        FixReport full = await RunAsync(fix, request, solution, [], cancellationToken);
        if (!fix.AllOrNothing || !full.Findings.Any(f => f.Result is ManualResult && !fix.AllowsPartial(f)))
        {
            return full;
        }

        // TAAK-274: the code tied to the Manual findings stays on the old library, the rest moves; otherwise nothing changes.
        if (fix is IPartialFix partial && await PartialRun.RunPartialAsync(partial, solution, full, (s, seed, ct) => RunAsync(fix, request, s, seed, ct), cancellationToken) is FixReport partialReport)
        {
            return partialReport;
        }

        return new FixReport
        {
            Solution = solution,
            Findings = [.. full.Findings.Select(f => f.Result is FixedResult ? FindingFactory.NotApplied(f) : f)],
            ProjectFileChanges = [],
            BlockingProjects = BlockingProjects(fix, solution, full.Findings),
        };
    }

    // Council round 18, X1/S2: per project (by file, a project-file row by its own path), the reasons of the Manual findings that hold the run back.
    private static List<BlockingProject> BlockingProjects(IFix fix, Solution solution, IReadOnlyList<Finding> findings)
    {
        Dictionary<string, string> owners = new(StringComparer.OrdinalIgnoreCase);
        foreach (Project project in solution.Projects)
        {
            string name = project.FilePath ?? project.Name;
            owners.TryAdd(name, name);
            foreach (Document document in project.Documents.Where(d => d.FilePath is not null))
            {
                owners.TryAdd(document.FilePath!, name);
            }
        }

        return
        [
            .. findings.Where(f => f.Result is ManualResult && !fix.AllowsPartial(f))
                .GroupBy(f => owners.GetValueOrDefault(f.FilePath) ?? f.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(g => new BlockingProject { Project = g.Key, Reasons = [.. g.Select(f => ((ManualResult)f.Result).Reason).Distinct(StringComparer.Ordinal)] }),
        ];
    }

    // One run of the fix over the solution; seed findings come first in the report (the rows of a partial plan).
    private async Task<FixReport> RunAsync(IFix fix, FixRequest request, Solution solution, IReadOnlyList<Finding> seed, CancellationToken cancellationToken)
    {
        var findings = new List<Finding>(seed);
        Solution original = solution;
        // TAAK-320: a project with a blocking project-level row (G4) keeps its documents as they are; its rows come first.
        Dictionary<string, string> blockedFiles = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> blockedProjects = new(StringComparer.OrdinalIgnoreCase);
        if (fix is IProjectFileMigration blockingMigration)
        {
            (List<Finding> blockerRows, blockedFiles, blockedProjects) = ProjectFileMigrations.Blockers(fix, blockingMigration, original);
            findings.AddRange(blockerRows);
        }

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
        // A file without any name of the old library gets no finding and no change: it skips the patterns (TAAK-298).
        HashSet<string>? candidates = request.CheckEveryFile || !fix.FindsOnlyOldLibraryCode ? null : await OldLibraryFiles.FindAsync(solution, fix, files, cancellationToken);
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

        // TAAK-298: files are independent (each is fixed against the baseline), so they run in parallel; each collects its own
        // rows and the rows are merged in file order, which gives the same report as one file at a time.
        List<IGrouping<string, DocumentId>> checkedFiles = [.. files.Where(f => candidates?.Contains(f.Key) != false)];
        var outcomes = new FileOutcome[checkedFiles.Count];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Degree(fix, request),
            CancellationToken = cancellationToken,
        };
        await Parallel.ForEachAsync(Enumerable.Range(0, checkedFiles.Count), options, async (i, ct) =>
            outcomes[i] = await FixFileCopiesAsync(fix, original, baseline, checkedFiles[i], swappedTypes, globalProjects, ct));
        for (int i = 0; i < checkedFiles.Count; i++)
        {
            FileOutcome outcome = outcomes[i];
            if (outcome.ClearsInactiveBranchRows)
            {
                // As one file at a time did: the row of this file may be a seed row too.
                findings.RemoveAll(f => IsInactiveBranchRow(f, checkedFiles[i].Key));
            }

            findings.AddRange(outcome.Findings);
            if (outcome.Text is SourceText text)
            {
                foreach (DocumentId copy in checkedFiles[i])
                {
                    solution = solution.WithDocumentText(copy, text);
                }
            }
        }

        solution = await SolutionChecks.SwapGlobalUsingsAsync(solution, fix, globalProjects, swappedTypes, notUtf8, findings, cancellationToken);
        if (blockedFiles.Count > 0)
        {
            solution = await KeepBlockedFilesAsync(original, solution, blockedFiles, findings, cancellationToken);
        }

        // A package that builds on the old library: its source goes in, the package goes out with the others (TAAK-194).
        VendoredPlan vendored = await VendoredPackages.PlanAsync(original, fix, cancellationToken);
        findings.AddRange(vendored.Findings);
        solution = vendored.AddTo(solution);
        IReadOnlyList<ProjectFileChange> projectFileChanges = [];
        IReadOnlyList<ProjectMigration> migrated = [];
        if (_projectFiles is not null && fix.Package is PackageMigration package)
        {
            (solution, projectFileChanges, migrated) = await MigrateProjectFilesAsync(
                _projectFiles, original, solution, fix, package, swappedTypes, vendored.Migrations, blockedProjects, findings, cancellationToken);
        }

        projectFileChanges = [.. vendored.Files, .. projectFileChanges];
        // TAAK-317: the fix's typed project/config-file edits, after the document pass and on top of the package migration.
        if (fix is IProjectFileMigration fileMigration && _projectFiles is IProjectFileEditor editor)
        {
            projectFileChanges = await ProjectFileMigrations.ApplyAsync(fix, fileMigration, editor, original, findings, projectFileChanges, cancellationToken);
        }

        // TAAK-329, G10: a package that came only with the removed one and is still used gets a direct, pinned reference.
        if (_assetsReader is not null && migrated.Count > 0 && _projectFiles is IProjectFileEditor transitiveEditor
            && await TransitivePackages.PlanAsync(solution, migrated, _assetsReader, cancellationToken) is TransitivePackages transitive)
        {
            // Council round 17 T3: a lost package a project reference brings at another version is Manual, not pinned.
            findings.AddRange(transitive.Manuals.Select(m => FindingFactory.CreateProjectFinding(m.ProjectPath, fix, m.Result)));
            if (transitive.HasEdits)
            {
                projectFileChanges = await ProjectFileMigrations.ApplyAsync(fix, transitive, transitiveEditor, original, findings, projectFileChanges, cancellationToken);
            }
        }

        findings.AddRange(ProjectInfo(fix, original));
        findings.AddRange(ProjectSummary(fix, original, findings));
        if (fix.ChangedCodeInfo is { Length: > 0 } changedCodeInfo && findings.Any(f => f.Line > 0 && f.Result is FixedResult))
        {
            findings.Add(FindingFactory.CreateFinding(original.FilePath ?? ".", 0, fix, ChangedCodeInfoPatternId, PatternResult.Note(changedCodeInfo)));
        }

        return new FixReport { Solution = solution, Findings = findings, ProjectFileChanges = projectFileChanges };
    }

    // TAAK-320: every document of a blocked project gets its original text back, and each Fixed row in it becomes Manual naming the blocker.
    // A file shared with an unblocked project stays too: half a file would not build in the blocked one. The package step then sees the old
    // code and keeps the old package in that project.
    private static async Task<Solution> KeepBlockedFilesAsync(
        Solution original, Solution solution, Dictionary<string, string> blockedFiles, List<Finding> findings, CancellationToken cancellationToken)
    {
        foreach (Document document in original.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents)
            .Where(d => blockedFiles.ContainsKey(d.FilePath ?? d.Name)))
        {
            solution = solution.WithDocumentText(document.Id, await document.GetTextAsync(cancellationToken));
        }

        for (int i = 0; i < findings.Count; i++)
        {
            if (findings[i].Result is FixedResult && blockedFiles.TryGetValue(findings[i].FilePath, out string? blocker))
            {
                findings[i] = FindingFactory.CreateFinding(findings[i], PatternResult.Manual($"{NotRewrittenPrefix}blocked by {blocker}. Resolve that first, or migrate this project by hand."));
            }
        }

        return solution;
    }

    // Council round 14, M11: at most this many dependents in the row text; the JSON report has them all.
    internal const int MaxDependentsInRow = 5;

    internal static string DependentList(IReadOnlyList<string> dependents) =>
        dependents.Count <= MaxDependentsInRow
            ? string.Join(", ", dependents)
            : $"{string.Join(", ", dependents.Take(MaxDependentsInRow))} and {dependents.Count - MaxDependentsInRow} more";

    // Council round 14, M10: the kept row covers the project file's own edit, so it names the packages that edit added next to the old one.
    private static string AddedNextTo(ProjectMigration migration, IReadOnlyList<ProjectFileChange> changes)
    {
        ProjectFileChange? change = changes.FirstOrDefault(c => string.Equals(c.FilePath, migration.ProjectPath, StringComparison.OrdinalIgnoreCase));
        List<string> added = change is null
            ? []
            : [.. migration.Package.AddedPackages.Select(p => p.Name).Where(name => !change.OldText.Contains(name, StringComparison.OrdinalIgnoreCase)
                && change.NewText.Contains(name, StringComparison.OrdinalIgnoreCase))];
        return added.Count == 0 ? string.Empty : $"; {string.Join(" and ", added)} {(added.Count == 1 ? "was" : "were")} added next to it";
    }

    // Council round 10, C2: the fix-level Info rows, once per project file (a multi-target project asks its first copy), in project order.
    internal static IEnumerable<Finding> ProjectInfo(IFix fix, Solution solution) =>
        solution.Projects
            .Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null)
            .GroupBy(p => p.FilePath!, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g => fix.ProjectInfo(g.First()).Select(text => FindingFactory.CreateProjectFinding(g.Key, fix, PatternResult.Note(text))));

    // TAAK-342, council round 17, B3: the fix's summary rows per project file, from the rows of that project's documents.
    internal static IEnumerable<Finding> ProjectSummary(IFix fix, Solution solution, IReadOnlyList<Finding> findings) =>
        solution.Projects
            .Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null)
            .GroupBy(p => p.FilePath!, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g =>
            {
                HashSet<string> documents = new(g.SelectMany(p => p.Documents).Select(d => d.FilePath ?? d.Name), StringComparer.OrdinalIgnoreCase);
                return fix.ProjectSummary([.. findings.Where(f => f.Line > 0 && documents.Contains(f.FilePath))])
                    .Select(result => FindingFactory.CreateProjectFinding(g.Key, fix, result));
            })
            .ToList();

    // One file with all its copies (one per target framework): its rows and its fixed text (null when it stays as it was).
    private static async Task<FileOutcome> FixFileCopiesAsync(
        IFix fix,
        Solution original,
        Solution baseline,
        IGrouping<string, DocumentId> file,
        HashSet<string> swappedTypes,
        HashSet<ProjectId> globalProjects,
        CancellationToken cancellationToken)
    {
        // TAAK-223: a copy per distinct set of active #if branches. The first pass fixes what is active in the first copy;
        // each later pass takes the text so far and fixes only the code that none of the earlier copies saw (`#if !NETFRAMEWORK`).
        List<Finding> findings = [];
        bool clears = false;
        SourceText? text = null;
        List<ParseOptions> earlier = [];
        foreach (DocumentId documentId in await PassCopiesAsync(original, file, cancellationToken))
        {
            Solution passBaseline = text is null ? baseline : baseline.WithDocumentText(documentId, text);
            (Solution working, bool usingsUpdated, int firstFinding) = await FixCopyAsync(
                fix, original, passBaseline, documentId, swappedTypes, globalProjects, earlier, findings, text, cancellationToken);
            if (earlier.Count > 0 && usingsUpdated)
            {
                // The usings of this pass replace the inactive-branch row of an earlier one: that code may be migrated now.
                clears = true;
                for (int i = firstFinding - 1; i >= 0; i--)
                {
                    if (IsInactiveBranchRow(findings[i], file.Key))
                    {
                        findings.RemoveAt(i);
                    }
                }
            }

            if (working != passBaseline)
            {
                text = await working.GetDocument(documentId)!.GetTextAsync(cancellationToken);
            }

            earlier.Add(original.GetDocument(documentId)!.Project.ParseOptions!);
        }

        return new FileOutcome { Findings = findings, Text = text, ClearsInactiveBranchRows = clears };
    }

    // The one rule for parallel files (council round 7): only when the request allows it and the fix promises thread safety.
    private static int Degree(IFix fix, FixRequest request) =>
        request.Sequential || !fix.FixesFilesInParallel ? 1 : Math.Max(1, request.MaxDegree ?? Math.Min(Environment.ProcessorCount, DefaultMaxDegree));

    private static bool IsInactiveBranchRow(Finding finding, string filePath) =>
        finding.PatternId == UsingsPatternId && finding.FilePath == filePath
        && finding.Result is ManualResult { Reason: var reason } && reason.StartsWith(InactiveBranchReason, StringComparison.Ordinal);

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
    private static async Task<(Solution Solution, IReadOnlyList<ProjectFileChange> Changes, IReadOnlyList<ProjectMigration> Migrations)> MigrateProjectFilesAsync(
        IProjectFileStore projectFiles,
        Solution original,
        Solution solution,
        IFix fix,
        PackageMigration package,
        HashSet<string> swappedTypes,
        IReadOnlyList<ProjectMigration> vendoredMigrations,
        Dictionary<string, string> blockedProjects,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        // One edit for both: the store reads each file once, so the second swap does not overwrite the first.
        IReadOnlyList<ProjectMigration> migrations =
            [.. await ProjectPackages.PlanAsync(original, solution, fix, package, swappedTypes, cancellationToken), .. vendoredMigrations];
        // TAAK-321: a blocked project (G4) gets no package edit at all: it keeps the old package and gets no new one next to it.
        if (blockedProjects.Count > 0)
        {
            foreach (string project in migrations.Select(m => m.ProjectPath).Where(blockedProjects.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                findings.Add(FindingFactory.CreateProjectFinding(project, fix, PatternResult.Manual(
                    $"{package.RemovedPackage} package kept: blocked by {blockedProjects[project]}. Resolve that first, then run the fix again.")));
            }

            migrations = [.. migrations.Where(m => !blockedProjects.ContainsKey(m.ProjectPath))];
        }

        // A project a partial plan keeps whole on the old library (EPPlus, E17) gets no new package next to the old one; its kept row explains it.
        HashSet<string> keptProjects = new(findings.Where(f => f.Line == 0 && f.Result is ManualResult { Kept: KeptReason.EPPlusProjectKept }).Select(f => f.FilePath), StringComparer.OrdinalIgnoreCase);
        migrations = [.. migrations.Where(m => !keptProjects.Contains(m.ProjectPath))];

        if (migrations.Count == 0)
        {
            return (solution, [], []);
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
            return (solution, [], []);
        }

        // Council round 21, B13: a fix that removes several packages names the one(s) the project references.
        string KeptName(string projectPath) => fix.KeptPackageName(projectPath) is { Length: > 0 } name ? name : package.RemovedPackage;
        findings.AddRange(migrations.Where(m => m.KeepRemovedPackage).Select(m => FindingFactory.CreateProjectFinding(m.ProjectPath, fix,
            m.KeptForDependents.Count > 0
                ? new ManualResult($"{KeptName(m.ProjectPath)}{KeptForDependentReason}{DependentList(m.KeptForDependents)}{AddedNextTo(m, changes)}. " +
                    (m.KeptForDependents.Count == 1 ? "Migrate that project and run the fix again." : "Migrate those projects and run the fix again.")) { Dependents = m.KeptForDependents }
                : PatternResult.Manual($"{KeptName(m.ProjectPath)}{KeptPackageReason}Fix the manual findings and run the fix again."))));
        // A kept project's own file edit (the new package next to the old one) is covered by its kept row: one row per project file.
        HashSet<string> keptRows = new(migrations.Where(m => m.KeepRemovedPackage).Select(m => m.ProjectPath), StringComparer.OrdinalIgnoreCase);
        // The project-file row alone says that something is left, not where. A file with a Manual finding already has its explanation.
        HashSet<string> removed = [package.RemovedPackage, .. package.AlsoRemovedPackages];
        HashSet<string> explained = [.. findings.Where(f => f.Result is ManualResult).Select(f => f.FilePath)];
        foreach (ProjectMigration kept in migrations.Where(m => m.KeepRemovedPackage))
        {
            IReadOnlyList<(string FilePath, int Line, string Symbol)> uses =
                await ProjectPackages.RemainingUsesAsync(solution, kept.ProjectPath, removed, swappedTypes, cancellationToken);
            if (fix.ReportsEveryRemainingUse)
            {
                AddRemainingUseRows(findings, fix, uses, KeptName(kept.ProjectPath));
                continue;
            }

            foreach ((string filePath, int line, string symbol) in uses)
            {
                if (!explained.Contains(filePath))
                {
                    findings.Add(FindingFactory.CreateFinding(filePath, line, fix, UnmatchedPatternId, PatternResult.Manual(
                        $"{symbol} is still used here and no pattern migrates it, so {package.RemovedPackage} stays in this project. Migrate it by hand.")));
                }
            }
        }

        findings.AddRange(changes.Where(c => c.KeptRemovedPackageFor is not null || !keptRows.Contains(c.FilePath)).Select(c =>FindingFactory.CreateProjectFinding(c.FilePath, fix, c.KeptRemovedPackageFor is string user
            ? PatternResult.Manual($"{user}{KeptSharedPackageReason}{package.RemovedPackage}, so this shared file keeps it next to the new package. " +
                "Migrate that project and run the fix again.")
            : PatternResult.Fixed(Array.Empty<SyntaxNode>()))));
        return (solution, changes, migrations);
    }

    // Council round 21, B14: one row per file with the count and every line of the uses outside a Manual row's line (that row already names
    // its call), also in a file that has a Manual row. The row sits on the first such line.
    private static void AddRemainingUseRows(List<Finding> findings, IFix fix, IReadOnlyList<(string FilePath, int Line, string Symbol)> uses, string keptName)
    {
        HashSet<(string, int)> manualLines = [.. findings.Where(f => f.FixId == fix.Id && f.Line > 0 && f.Result is ManualResult).Select(f => (f.FilePath, f.Line))];
        foreach (IGrouping<string, int> file in uses.Where(u => !manualLines.Contains((u.FilePath, u.Line))).GroupBy(u => u.FilePath, u => u.Line))
        {
            List<int> lines = [.. file.Distinct().Order()];
            string usages = lines.Count == 1 ? "1 other usage" : $"{lines.Count} other usages";
            findings.Add(FindingFactory.CreateFinding(file.Key, lines[0], fix, UnmatchedPatternId, PatternResult.Manual(
                $"{usages} of {keptName} that no pattern migrates: line{(lines.Count == 1 ? string.Empty : "s")} {string.Join(", ", lines)}. " +
                "The package stays in this project; migrate them by hand.")));
        }
    }
}
