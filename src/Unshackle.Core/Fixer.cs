using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

public class Fixer : IFixer
{
    // Rows for project files have no pattern; this id groups them in the summary and the report.
    public const string ProjectFilePatternId = "project-file";

    // Rows for usings the fix could not swap safely: a global using, or a using that inactive #if code may need.
    public const string UsingsPatternId = "usings";

    // Rows for code that still names the removed package while no pattern found it: the place the user has to migrate by hand.
    public const string UnmatchedPatternId = "unmatched-use";

    // Rows for a source file that is not UTF-8: writing it back as UTF-8 would corrupt its characters, so it is left alone.
    public const string EncodingPatternId = "encoding";

    // TAAK-198: each round of targeted imports can only add conflicts; real files settle in two or three.
    private const int MaxTargetedRounds = 8;

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

        var findings = new List<Finding>();
        Solution original = solution;
        Solution baseline = AddReferences(original, fix.AddedReferences, fix.AddedNamespaces);
        HashSet<string> swappedTypes = [.. fix.SwappedTypes];
        // A file that is not UTF-8 stays as it is, with one Manual row (TAAK-167). It still names the old library, so its
        // project keeps the package (the project-file cascade below) and the rest of the solution is migrated.
        HashSet<string> notUtf8 = await NotUtf8FilesAsync(solution, cancellationToken);
        // Only a file that names the old library needs the row; another one is left alone without noise.
        foreach (string path in notUtf8.Order(StringComparer.Ordinal))
        {
            Document document = solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents).First(d => d.FilePath == path);
            if (await UsingDirectives.NamesRemovedNamespaceAsync(document, fix.RemovedNamespaces, cancellationToken))
            {
                findings.Add(CreateFinding(path, 0, fix, EncodingPatternId, PatternResult.Manual(
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

        // Only built when a file conflicts with an added namespace (TAAK-198): references without the global using, so a check sees
        // the imports the file itself has.
        Solution? unimported = null;
        foreach (IGrouping<string, DocumentId> file in files)
        {
            DocumentId documentId = file.First();
            int firstFinding = findings.Count;
            FilePass pass = await FixFileAsync(fix, original, baseline, documentId, swappedTypes, globalProjects, s_noneBlocked, false, findings, cancellationToken);
            Solution working = pass.Working;
            if (pass.Updated is Document updated)
            {
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
                    && await UsingDirectives.MisboundAddedCallAsync(baseline.GetDocument(documentId)!, updated, fix.AddedNamespaces, cancellationToken) is { } misbound)
                {
                    // An own extension in the enclosing namespace wins without ambiguity: the rewrite would silently change behaviour (TAAK-199).
                    conflict = ($"a rewritten {misbound.Bound.Name}() binds to {misbound.Bound.ContainingType.ToDisplayString()}.{misbound.Bound.Name} instead of "
                        + $"{string.Join(", ", fix.AddedNamespaces)} (an own method in the enclosing namespace or type wins over an imported extension), "
                        + "so this file is not migrated and its project keeps the old package. Rename or remove the conflicting method, or migrate the file by hand.",
                        misbound.Line);
                }

                if (conflict is { } c)
                {
                    // First try to migrate the rest of the file: only the classes that do not conflict are imported, and the calls
                    // that would conflict stay as they were (TAAK-198). When that does not work out, the whole file stays (TAAK-174).
                    List<Finding> firstPass = findings[firstFinding..];
                    unimported ??= AddReferences(original, fix.AddedReferences, []);
                    Solution? targeted = globalProjects.Contains(documentId.ProjectId)
                        ? null
                        : await FixWithTargetedImportsAsync(fix, original, baseline, unimported, documentId, swappedTypes, globalProjects, findings, firstFinding, cancellationToken);
                    if (targeted is not null)
                    {
                        working = targeted;
                    }
                    else
                    {
                        findings.RemoveRange(firstFinding, findings.Count - firstFinding);
                        findings.AddRange(firstPass);
                        for (int i = firstFinding; i < findings.Count; i++)
                        {
                            if (findings[i].Result is FixedResult)
                            {
                                findings[i] = CreateFinding(findings[i], PatternResult.Manual($"Not rewritten: {c.Reason}"));
                            }
                        }

                        findings.Add(CreateUsingsFinding(updated.FilePath ?? updated.Name, c.Line, fix, PatternResult.Manual(c.Reason)));
                        working = baseline;
                    }
                }
            }

            if (working != baseline)
            {
                SourceText text = await working.GetDocument(documentId)!.GetTextAsync(cancellationToken);
                foreach (DocumentId copy in file)
                {
                    solution = solution.WithDocumentText(copy, text);
                }
            }
        }

        solution = await SwapGlobalUsingsAsync(solution, fix, globalProjects, swappedTypes, notUtf8, findings, cancellationToken);
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

        if (fix.AllOrNothing && findings.Any(f => f.Result is ManualResult))
        {
            return new FixReport { Solution = original, Findings = [.. findings.Select(f => f.Result is FixedResult ? NotApplied(f) : f)], ProjectFileChanges = [] };
        }

        return new FixReport { Solution = solution, Findings = findings, ProjectFileChanges = projectFileChanges };
    }

    // One pass over a file: the patterns, then the usings. A rewrite that calls a blocked name is Manual with the blocked reason;
    // targeted leaves the added namespaces out of the usings (the caller imports the classes that do not conflict, TAAK-198).
    // Updated is null when the usings were not touched.
    private static async Task<FilePass> FixFileAsync(
        IFix fix,
        Solution original,
        Solution baseline,
        DocumentId documentId,
        HashSet<string> swappedTypes,
        HashSet<ProjectId> globalProjects,
        IReadOnlyDictionary<string, string> blocked,
        bool targeted,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        // Every file is fixed against the original solution, so what a pattern finds never depends on which files
        // were fixed before (a marker interface in an earlier file would already be swapped, TAAK-130).
        Solution working = baseline;
        var requiredNamespaces = new HashSet<string>(StringComparer.Ordinal);
        bool changed = false;
        // Patterns run one after another, each on a fresh semantic model of the previous result.
        foreach (IPattern pattern in fix.Patterns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Document document = working.GetDocument(documentId)!;
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
            string filePath = document.FilePath ?? document.Name;
            SyntaxNode newRoot = await ApplyPatternAsync(fix, pattern, filePath, root, model, document.Project.Solution, blocked, findings, requiredNamespaces, cancellationToken);
            if (newRoot != root)
            {
                working = working.WithDocumentSyntaxRoot(documentId, newRoot);
                changed = true;
            }
        }

        // A file that only uses swapped types needs no pattern, just the using swap. A file without findings still loses
        // its unused old using: the package may be removed from the project (TAAK-073).
        if (!changed
            && !await UsingDirectives.HasUnusedRemovedUsingAsync(working.GetDocument(documentId)!, fix.RemovedNamespaces, swappedTypes, cancellationToken))
        {
            return new FilePass { Working = working, Changed = changed, ImportsAdded = false, Updated = null };
        }

        // Only a file that names the old library gets the new namespaces: one that uses it through a base type of its own
        // would get an unused using (IDE0005, TAAK-115).
        if ((changed || swappedTypes.Count > 0)
            && await UsingDirectives.NamesRemovedNamespaceAsync(original.GetDocument(documentId)!, fix.RemovedNamespaces, cancellationToken))
        {
            requiredNamespaces.UnionWith(fix.AddedNamespaces);
        }

        if (globalProjects.Contains(documentId.ProjectId) || targeted)
        {
            // The project's global using brings the added namespaces (see SwapGlobalUsingsAsync), or the caller imports them per class.
            requiredNamespaces.ExceptWith(fix.AddedNamespaces);
        }

        Document current = working.GetDocument(documentId)!;
        working = await UsingDirectives.UpdateAsync(
            current, requiredNamespaces, fix.RemovedNamespaces, swappedTypes, fix.RemovedExtensionMethods, fix.AddedTypes, cancellationToken);
        Document updated = working.GetDocument(documentId)!;
        SyntaxNode updatedRoot = (await updated.GetSyntaxRootAsync(cancellationToken))!;
        SemanticModel currentModel = (await current.GetSemanticModelAsync(cancellationToken))!;
        findings.AddRange(UsingDirectives.UnboundCalls((await current.GetSyntaxRootAsync(cancellationToken))!, currentModel, fix.RemovedExtensionMethods, cancellationToken)
            .Select(n => CreateUsingsFinding(updated.FilePath ?? updated.Name, n.GetLocation().GetLineSpan().StartLinePosition.Line + 1, fix, PatternResult.Manual(
                $"{n.Identifier.ValueText}() binds to nothing here (the type it is called on is unknown, e.g. after a rewrite in a lambda earlier on), " +
                "so no pattern migrated it and the old using stays. Run the fix again once the project builds, or migrate it by hand."))));
        if (UsingDirectives.KeepsRemovedUsing(updatedRoot, currentModel.Compilation, fix.RemovedNamespaces))
        {
            // Line in the file as the patterns saw it, like their findings; 0 (whole file) when only the updated root has it.
            SyntaxTrivia disabled = (await current.GetSyntaxRootAsync(cancellationToken))!.DescendantTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.DisabledTextTrivia));
            int line = disabled.IsKind(SyntaxKind.DisabledTextTrivia) ? disabled.GetLocation().GetLineSpan().StartLinePosition.Line + 1 : 0;
            findings.Add(CreateUsingsFinding(updated.FilePath ?? updated.Name, line, fix, PatternResult.Manual(
                "Code in an inactive #if branch is not migrated, so the old using stays. Migrate that branch by hand.")));
        }

        return new FilePass { Working = working, Changed = changed, ImportsAdded = requiredNamespaces.Count > 0, Updated = updated };
    }

    // TAAK-198: a file whose rewrite conflicts with an own extension (Akka.TestKit's ShouldBe next to Shouldly's) is migrated
    // anyway. Instead of the added namespace it gets `using static` for each added class it calls, minus the classes that make a
    // call ambiguous; rewrites that would call a conflicting name (or a name of a left-out class) stay as they were, as Manual.
    // Each round may find a new conflict; the sets only grow. Null when it does not work out (the caller keeps the whole file).
    private static async Task<Solution?> FixWithTargetedImportsAsync(
        IFix fix,
        Solution original,
        Solution baseline,
        Solution unimported,
        DocumentId documentId,
        HashSet<string> swappedTypes,
        HashSet<ProjectId> globalProjects,
        List<Finding> findings,
        int firstFinding,
        CancellationToken cancellationToken)
    {
        var blocked = new Dictionary<string, string>(StringComparer.Ordinal);
        var leftOut = new Dictionary<string, string>(StringComparer.Ordinal);
        Document originalCheck = unimported.GetDocument(documentId)!;
        int originalErrors = await UsingDirectives.ErrorCountAsync(originalCheck, cancellationToken);
        for (int round = 0; round < MaxTargetedRounds; round++)
        {
            findings.RemoveRange(firstFinding, findings.Count - firstFinding);
            FilePass pass = await FixFileAsync(fix, original, baseline, documentId, swappedTypes, globalProjects, blocked, true, findings, cancellationToken);
            Document document = pass.Working.GetDocument(documentId)!;
            if (await UsingDirectives.AddedUsageAsync(document, fix.AddedNamespaces, cancellationToken) is not AddedUsage usage)
            {
                return null;
            }

            bool grew = false;
            foreach ((string name, string type) in usage.Calls)
            {
                if (leftOut.TryGetValue(type, out string? why) && !blocked.ContainsKey(name))
                {
                    blocked[name] = $"{name}() is in {type}, which is not imported here: {why}";
                    grew = true;
                }
            }

            if (grew)
            {
                continue;
            }

            CompilationUnitSyntax root = UsingDirectives.AddTargetedImports(
                (CompilationUnitSyntax)(await document.GetSyntaxRootAsync(cancellationToken))!, usage.Calls.Select(c => c.Type).Where(type => !leftOut.ContainsKey(type)), usage.Types);
            Document candidate = unimported.GetDocument(documentId)!.WithSyntaxRoot(root);
            IReadOnlyList<AddedConflict> conflicts = await UsingDirectives.ConflictsWithAddedAsync(originalCheck, candidate, fix.AddedNamespaces, cancellationToken);
            if (conflicts.Count == 0)
            {
                // Anything else the imports broke (a missed type, an alias that clashes): the whole file stays.
                return await UsingDirectives.ErrorCountAsync(candidate, cancellationToken) > originalErrors
                    ? null
                    : pass.Working.WithDocumentSyntaxRoot(documentId, root);
            }

            foreach (AddedConflict conflict in conflicts)
            {
                grew |= conflict.AddedType is string type && leftOut.TryAdd(type, conflict.Reason);
                grew |= blocked.TryAdd(conflict.Name, conflict.Reason);
            }

            if (!grew)
            {
                return null;
            }
        }

        return null;
    }

    // Only for the semantic model the patterns see: the added namespaces are imported by a global using in an extra document,
    // because a file gets its new usings after all patterns ran. A project that already references the assembly (by file name)
    // keeps its own copy.
    private static Solution AddReferences(Solution solution, IReadOnlyList<MetadataReference> references, IReadOnlyList<string> namespaces)
    {
        if (references.Count == 0)
        {
            return solution;
        }

        string usings = string.Concat(namespaces.Select(ns => $"global using {ns};\n"));
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).ToList())
        {
            solution = solution.AddDocument(
                DocumentId.CreateNewId(project.Id), UsingDirectives.AddedUsingsPath, SourceText.From(usings), filePath: UsingDirectives.AddedUsingsPath);
            HashSet<string> present = [.. project.MetadataReferences.Select(r => Path.GetFileName(r.Display ?? string.Empty))];
            List<MetadataReference> missing = [.. references.Where(r => !present.Contains(Path.GetFileName(r.Display ?? string.Empty)))];
            if (missing.Count > 0)
            {
                solution = solution.AddMetadataReferences(project.Id, missing);
            }
        }

        return solution;
    }

    private static Finding NotApplied(Finding finding) => CreateFinding(finding, PatternResult.Manual(
        "Not rewritten: this fix changes nothing while other findings are Manual (a partial migration would break at run time). " +
        "Fix the manual findings and run the fix again."));

    private static Finding CreateFinding(Finding finding, PatternResult result) => new()
    {
        FilePath = finding.FilePath,
        Line = finding.Line,
        FixId = finding.FixId,
        PatternId = finding.PatternId,
        Result = result,
    };

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
                    findings[i] = CreateFinding(findings[i], PatternResult.Manual($"Not rewritten: {reason}"));
                }
            }

            findings.AddRange(migrations.Select(m => CreateProjectFinding(m.ProjectPath, fix, PatternResult.Manual(reason))));
            return (solution, []);
        }

        findings.AddRange(migrations.Where(m => m.KeepRemovedPackage).Select(m => CreateProjectFinding(m.ProjectPath, fix,
            PatternResult.Manual($"{package.RemovedPackage} is still used in this project, so its package and global usings stay. " +
                "Fix the manual findings and run the fix again."))));
        // The project-file row alone says that something is left, not where. A file with a Manual finding already has its explanation.
        HashSet<string> removed = [package.RemovedPackage, .. package.AlsoRemovedPackages];
        HashSet<string> explained = [.. findings.Where(f => f.Result is ManualResult).Select(f => f.FilePath)];
        foreach (ProjectMigration kept in migrations.Where(m => m.KeepRemovedPackage))
        {
            foreach ((string filePath, int line, string symbol) in await ProjectPackages.RemainingUsesAsync(solution, kept.ProjectPath, removed, swappedTypes, cancellationToken))
            {
                if (!explained.Contains(filePath))
                {
                    findings.Add(CreateFinding(filePath, line, fix, UnmatchedPatternId, PatternResult.Manual(
                        $"{symbol} is still used here and no pattern migrates it, so {package.RemovedPackage} stays in this project. Migrate it by hand.")));
                }
            }
        }

        findings.AddRange(changes.Select(c => CreateProjectFinding(c.FilePath, fix, c.KeptRemovedPackageFor is string user
            ? PatternResult.Manual($"{user} is not migrated but still uses {package.RemovedPackage}, so this shared file keeps it next to the new package. " +
                "Migrate that project and run the fix again.")
            : PatternResult.Fixed(Array.Empty<SyntaxNode>()))));
        return (solution, changes);
    }

    // A global using applies to the whole project (every target): it is swapped only when no file still needs it.
    // Otherwise the added namespaces are imported next to it, so the migrated files still compile.
    private static async Task<Solution> SwapGlobalUsingsAsync(
        Solution solution,
        IFix fix,
        HashSet<ProjectId> globalProjects,
        HashSet<string> swappedTypes,
        HashSet<string> notUtf8,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        foreach (IGrouping<string, ProjectId> project in globalProjects.GroupBy(id => solution.GetProject(id)!.FilePath ?? id.Id.ToString(), StringComparer.Ordinal))
        {
            bool keep = false;
            foreach (Document document in project.SelectMany(id => solution.GetProject(id)!.Documents))
            {
                keep = keep || await UsingDirectives.MayUseRemovedNamespaceAsync(document, fix.RemovedNamespaces, swappedTypes, cancellationToken);
            }

            foreach (DocumentId documentId in project.SelectMany(id => solution.GetProject(id)!.DocumentIds).ToList())
            {
                if (notUtf8.Contains(solution.GetDocument(documentId)!.FilePath ?? documentId.Id.ToString()))
                {
                    continue;
                }

                solution = await UsingDirectives.QualifyClashingAsync(
                    solution.GetDocument(documentId)!, fix.AddedNamespaces, fix.RemovedNamespaces, fix.AddedTypes, cancellationToken);
            }

            HashSet<string> reported = new(StringComparer.Ordinal);
            foreach (Document document in project.SelectMany(id => solution.GetProject(id)!.Documents).Where(d => !notUtf8.Contains(d.FilePath ?? d.Id.Id.ToString())))
            {
                SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
                List<UsingDirectiveSyntax> globals = [.. UsingDirectives.RemovedGlobalUsings(root, document.FilePath, fix.RemovedNamespaces)];
                if (globals.Count == 0)
                {
                    continue;
                }

                solution = solution.WithDocumentSyntaxRoot(document.Id, UsingDirectives.SwapGlobalUsings((CompilationUnitSyntax)root, globals, fix.AddedNamespaces, keep));
                string filePath = document.FilePath ?? document.Name;
                if (keep && reported.Add(filePath))
                {
                    findings.AddRange(globals.Select(g => CreateUsingsFinding(filePath, g.GetLocation().GetLineSpan().StartLinePosition.Line + 1, fix,
                        PatternResult.Manual($"The project still uses {g.Name} (or has inactive #if code), so its global using stays next to the new one. " +
                            "Fix the manual findings and run the fix again."))));
                }
            }
        }

        return solution;
    }

    // Roslyn reads a file with a BOM or valid UTF-8 as UTF-8, and anything else (Latin-1, UTF-16) with that other encoding.
    // Text without an encoding (made in memory) is written as UTF-8.
    private static async Task<HashSet<string>> NotUtf8FilesAsync(Solution solution, CancellationToken cancellationToken)
    {
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (document.FilePath is not null && (await document.GetTextAsync(cancellationToken)).Encoding is { } encoding and not UTF8Encoding)
            {
                paths.Add(document.FilePath);
            }
        }

        return paths;
    }

    private static Finding CreateUsingsFinding(string filePath, int line, IFix fix, PatternResult result) =>
        new() { FilePath = filePath, Line = line, FixId = fix.Id, PatternId = UsingsPatternId, Result = result };

    private static Finding CreateProjectFinding(string filePath, IFix fix, PatternResult result) =>
        new() { FilePath = filePath, Line = 0, FixId = fix.Id, PatternId = ProjectFilePatternId, Result = result };

    // Returns the same root instance when nothing was rewritten.
    private static async Task<SyntaxNode> ApplyPatternAsync(
        IFix fix,
        IPattern pattern,
        string filePath,
        SyntaxNode root,
        SemanticModel model,
        Solution solution,
        IReadOnlyDictionary<string, string> blocked,
        List<Finding> findings,
        HashSet<string> requiredNamespaces,
        CancellationToken cancellationToken)
    {
        List<SyntaxNode> nodes;
        try
        {
            // Outer nodes first: a nested finding inside an already replaced node is reported, not lost.
            IEnumerable<SyntaxNode> found = pattern is ISolutionFinder finder
                ? await finder.FindAsync(root, model, solution, cancellationToken)
                : pattern.Find(root, model);
            nodes = [.. found.OrderBy(n => n.SpanStart).ThenByDescending(n => n.Span.Length)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(CreateFinding(filePath, 0, fix, pattern, InternalError(pattern, ex)));
            return root;
        }

        if (nodes.Count == 0)
        {
            return root;
        }

        SyntaxNode newRoot = root.TrackNodes(nodes);
        bool changed = false;
        foreach (SyntaxNode node in nodes)
        {
            PatternResult result = await RewriteAsync(pattern, node, model, solution, cancellationToken);
            if (BlockedCall(result, blocked) is string reason)
            {
                result = PatternResult.Manual($"Not rewritten: {reason}");
            }

            (newRoot, PatternResult reported) = result switch
            {
                FixedResult fixedResult => Replace(newRoot, node, fixedResult, pattern),
                ManualResult => (newRoot, result),
                _ => throw new UnreachableException(),
            };

            if (reported is FixedResult applied)
            {
                changed = true;
                requiredNamespaces.UnionWith(applied.RequiredNamespaces);
            }

            int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(CreateFinding(filePath, line, fix, pattern, reported));
        }

        return changed ? newRoot : root;
    }

    // The reason of the first call in the rewrite to a name that conflicts with an own extension (TAAK-198); null when there is none.
    private static string? BlockedCall(PatternResult result, IReadOnlyDictionary<string, string> blocked) =>
        blocked.Count == 0 || result is not FixedResult fixedResult
            ? null
            : fixedResult.NewNodes.SelectMany(n => n.DescendantNodesAndSelf()).OfType<InvocationExpressionSyntax>()
                .Select(i => i.Expression switch
                {
                    MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                    MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
                    SimpleNameSyntax name => name.Identifier.ValueText,
                    _ => null,
                })
                .Select(name => name is not null && blocked.TryGetValue(name, out string? reason) ? reason : null)
                .FirstOrDefault(reason => reason is not null);

    private static async Task<PatternResult> RewriteAsync(IPattern pattern, SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        try
        {
            return pattern is ISolutionPattern solutionPattern
                ? await solutionPattern.RewriteAsync(node, model, solution, cancellationToken)
                : pattern.Rewrite(node, model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return InternalError(pattern, ex);
        }
    }

    private static (SyntaxNode Root, PatternResult Result) Replace(SyntaxNode root, SyntaxNode node, FixedResult result, IPattern pattern)
    {
        SyntaxNode? current = root.GetCurrentNode(node);
        if (current is null)
        {
            return (root, PatternResult.Manual("Overlaps an earlier finding of this pattern; run the fix again."));
        }

        try
        {
            return (ReplaceWithTrivia(root, current, result.NewNodes), result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (root, InternalError(pattern, ex));
        }
    }

    private static SyntaxNode ReplaceWithTrivia(SyntaxNode root, SyntaxNode current, IReadOnlyList<SyntaxNode> newNodes)
    {
        if (newNodes.Count == 1)
        {
            return root.ReplaceNode(current, newNodes[0].WithTriviaFrom(current));
        }

        // Extra nodes get only the indentation, so comments in front of the original are not duplicated;
        // blank lines a pattern put in front of an extra node stay (a new type after a class).
        SyntaxTriviaList leading = current.GetLeadingTrivia();
        SyntaxTriviaList indentation = [.. leading.Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse()];
        IEnumerable<SyntaxNode> withTrivia = newNodes.Select((n, i) => n
            .WithLeadingTrivia(i == 0 ? leading : [.. n.GetLeadingTrivia().TakeWhile(t => t.IsKind(SyntaxKind.EndOfLineTrivia)), .. indentation])
            .WithTrailingTrivia(current.GetTrailingTrivia()));
        return root.ReplaceNode(current, withTrivia);
    }

    private static PatternResult InternalError(IPattern pattern, Exception ex) =>
        PatternResult.Manual($"Internal error in {pattern.Id}: {ex.Message}");

    private static Finding CreateFinding(string filePath, int line, IFix fix, IPattern pattern, PatternResult result) =>
        CreateFinding(filePath, line, fix, pattern.Id, result);

    private static Finding CreateFinding(string filePath, int line, IFix fix, string patternId, PatternResult result) =>
        new() { FilePath = filePath, Line = line, FixId = fix.Id, PatternId = patternId, Result = result };
}
