using System.Diagnostics;
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
        HashSet<string> swappedTypes = [.. fix.SwappedTypes];
        // A multi-target project is loaded once per target framework: the same file is fixed and reported once,
        // and its copies in the other projects get the fixed text.
        List<IGrouping<string, DocumentId>> files =
        [
            .. solution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                .SelectMany(p => p.Documents)
                .GroupBy(d => d.FilePath ?? d.Id.Id.ToString(), d => d.Id, StringComparer.Ordinal),
        ];
        HashSet<ProjectId> globalProjects = [];
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            if (UsingDirectives.RemovedGlobalUsings(root, document.FilePath, fix.RemovedNamespaces).Any())
            {
                globalProjects.Add(document.Project.Id);
            }
        }

        foreach (IGrouping<string, DocumentId> file in files)
        {
            DocumentId documentId = file.First();
            var requiredNamespaces = new HashSet<string>(StringComparer.Ordinal);
            bool changed = false;
            // Patterns run one after another, each on a fresh semantic model of the previous result.
            foreach (IPattern pattern in fix.Patterns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Document document = solution.GetDocument(documentId)!;
                SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
                SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
                string filePath = document.FilePath ?? document.Name;
                SyntaxNode newRoot = ApplyPattern(fix, pattern, filePath, root, model, findings, requiredNamespaces);
                if (newRoot != root)
                {
                    solution = solution.WithDocumentSyntaxRoot(documentId, newRoot);
                    changed = true;
                }
            }

            // A file that only uses swapped types needs no pattern, just the using swap. A file without findings still loses
            // its unused old using: the package may be removed from the project (TAAK-073).
            if (changed
                || await UsingDirectives.HasUnusedRemovedUsingAsync(solution.GetDocument(documentId)!, fix.RemovedNamespaces, swappedTypes, cancellationToken))
            {
                if (changed || swappedTypes.Count > 0)
                {
                    requiredNamespaces.UnionWith(fix.AddedNamespaces);
                }

                if (globalProjects.Contains(documentId.ProjectId))
                {
                    // The project's global using brings the added namespaces (see SwapGlobalUsingsAsync).
                    requiredNamespaces.ExceptWith(fix.AddedNamespaces);
                }

                Document document = solution.GetDocument(documentId)!;
                solution = await UsingDirectives.UpdateAsync(document, requiredNamespaces, fix.RemovedNamespaces, swappedTypes, cancellationToken);
                Document updated = solution.GetDocument(documentId)!;
                SyntaxNode updatedRoot = (await updated.GetSyntaxRootAsync(cancellationToken))!;
                if (UsingDirectives.KeepsRemovedUsing(updatedRoot, fix.RemovedNamespaces))
                {
                    // Line in the file as the patterns saw it, like their findings; 0 (whole file) when only the updated root has it.
                    SyntaxTrivia disabled = (await document.GetSyntaxRootAsync(cancellationToken))!.DescendantTrivia()
                        .FirstOrDefault(t => t.IsKind(SyntaxKind.DisabledTextTrivia));
                    int line = disabled.IsKind(SyntaxKind.DisabledTextTrivia) ? disabled.GetLocation().GetLineSpan().StartLinePosition.Line + 1 : 0;
                    findings.Add(CreateUsingsFinding(updated.FilePath ?? updated.Name, line, fix, PatternResult.Manual(
                        "Code in an inactive #if branch is not migrated, so the old using stays. Migrate that branch by hand.")));
                }

                SourceText text = await updated.GetTextAsync(cancellationToken);
                foreach (DocumentId copy in file.Skip(1))
                {
                    solution = solution.WithDocumentText(copy, text);
                }
            }
        }

        solution = await SwapGlobalUsingsAsync(solution, fix, globalProjects, swappedTypes, findings, cancellationToken);
        IReadOnlyList<ProjectFileChange> projectFileChanges = _projectFiles is not null && fix.Package is PackageMigration package
            ? await MigrateProjectFilesAsync(_projectFiles, solution, fix, package, swappedTypes, findings, cancellationToken)
            : [];
        if (fix.AllOrNothing && findings.Any(f => f.Result is ManualResult))
        {
            return new FixReport { Solution = original, Findings = [.. findings.Select(f => f.Result is FixedResult ? NotApplied(f) : f)], ProjectFileChanges = [] };
        }

        return new FixReport { Solution = solution, Findings = findings, ProjectFileChanges = projectFileChanges };
    }

    private static Finding NotApplied(Finding finding) => new()
    {
        FilePath = finding.FilePath,
        Line = finding.Line,
        FixId = finding.FixId,
        PatternId = finding.PatternId,
        Result = PatternResult.Manual("Not rewritten: this fix changes nothing while other findings are Manual (a partial migration would break at run time). " +
            "Fix the manual findings and run the fix again."),
    };

    // Runs on the fixed solution: only code that is still there can keep the removed package.
    private static async Task<IReadOnlyList<ProjectFileChange>> MigrateProjectFilesAsync(
        IProjectFileStore projectFiles,
        Solution solution,
        IFix fix,
        PackageMigration package,
        HashSet<string> swappedTypes,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProjectMigration> migrations = await ProjectPackages.PlanAsync(solution, fix, package, swappedTypes, cancellationToken);
        if (migrations.Count == 0)
        {
            return [];
        }

        IReadOnlyList<ProjectFileChange> changes;
        try
        {
            changes = await projectFiles.EditAsync(migrations, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Like a pattern: a broken project file is manual work, not a lost run.
            findings.AddRange(migrations.Select(m => CreateProjectFinding(m.ProjectPath, fix,
                PatternResult.Manual($"Could not edit the project files: {ex.Message}"))));
            return [];
        }

        findings.AddRange(migrations.Where(m => m.KeepRemovedPackage).Select(m => CreateProjectFinding(m.ProjectPath, fix,
            PatternResult.Manual($"{package.RemovedPackage} is still used in this project, so its package and global usings stay. " +
                "Fix the manual findings and run the fix again."))));
        findings.AddRange(changes.Select(c => CreateProjectFinding(c.FilePath, fix, PatternResult.Fixed(Array.Empty<SyntaxNode>()))));
        return changes;
    }

    // A global using applies to the whole project (every target): it is swapped only when no file still needs it.
    // Otherwise the added namespaces are imported next to it, so the migrated files still compile.
    private static async Task<Solution> SwapGlobalUsingsAsync(
        Solution solution,
        IFix fix,
        HashSet<ProjectId> globalProjects,
        HashSet<string> swappedTypes,
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

            HashSet<string> reported = new(StringComparer.Ordinal);
            foreach (Document document in project.SelectMany(id => solution.GetProject(id)!.Documents))
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

    private static Finding CreateUsingsFinding(string filePath, int line, IFix fix, PatternResult result) =>
        new() { FilePath = filePath, Line = line, FixId = fix.Id, PatternId = UsingsPatternId, Result = result };

    private static Finding CreateProjectFinding(string filePath, IFix fix, PatternResult result) =>
        new() { FilePath = filePath, Line = 0, FixId = fix.Id, PatternId = ProjectFilePatternId, Result = result };

    // Returns the same root instance when nothing was rewritten.
    private static SyntaxNode ApplyPattern(
        IFix fix,
        IPattern pattern,
        string filePath,
        SyntaxNode root,
        SemanticModel model,
        List<Finding> findings,
        HashSet<string> requiredNamespaces)
    {
        List<SyntaxNode> nodes;
        try
        {
            // Outer nodes first: a nested finding inside an already replaced node is reported, not lost.
            nodes = [.. pattern.Find(root, model).OrderBy(n => n.SpanStart).ThenByDescending(n => n.Span.Length)];
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
            PatternResult result = Rewrite(pattern, node, model);
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

    private static PatternResult Rewrite(IPattern pattern, SyntaxNode node, SemanticModel model)
    {
        try
        {
            return pattern.Rewrite(node, model);
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

        // Extra nodes get only the indentation, so comments in front of the original are not duplicated.
        SyntaxTriviaList leading = current.GetLeadingTrivia();
        SyntaxTriviaList indentation = [.. leading.Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse()];
        IEnumerable<SyntaxNode> withTrivia = newNodes.Select((n, i) => n
            .WithLeadingTrivia(i == 0 ? leading : indentation)
            .WithTrailingTrivia(current.GetTrailingTrivia()));
        return root.ReplaceNode(current, withTrivia);
    }

    private static PatternResult InternalError(IPattern pattern, Exception ex) =>
        PatternResult.Manual($"Internal error in {pattern.Id}: {ex.Message}");

    private static Finding CreateFinding(string filePath, int line, IFix fix, IPattern pattern, PatternResult result) =>
        new() { FilePath = filePath, Line = line, FixId = fix.Id, PatternId = pattern.Id, Result = result };
}
