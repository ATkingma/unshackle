using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

public class Fixer : IFixer
{
    // Rows for project files have no pattern; this id groups them in the summary and the report.
    public const string ProjectFilePatternId = "project-file";

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
        // A multi-target project is loaded once per target framework: the same file is fixed and reported once,
        // and its copies in the other projects get the fixed text.
        List<IGrouping<string, DocumentId>> files =
        [
            .. solution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                .SelectMany(p => p.Documents)
                .GroupBy(d => d.FilePath ?? d.Id.Id.ToString(), d => d.Id, StringComparer.Ordinal),
        ];
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

            if (changed)
            {
                requiredNamespaces.UnionWith(fix.AddedNamespaces);
                Document document = solution.GetDocument(documentId)!;
                solution = await UsingDirectives.UpdateAsync(document, requiredNamespaces, fix.RemovedNamespaces, cancellationToken);
                SourceText text = await solution.GetDocument(documentId)!.GetTextAsync(cancellationToken);
                foreach (DocumentId copy in file.Skip(1))
                {
                    solution = solution.WithDocumentText(copy, text);
                }
            }
        }

        IReadOnlyList<ProjectFileChange> projectFileChanges = _projectFiles is not null && fix.Package is PackageMigration package
            ? await MigrateProjectFilesAsync(_projectFiles, solution, fix, package, findings, cancellationToken)
            : [];
        return new FixReport { Solution = solution, Findings = findings, ProjectFileChanges = projectFileChanges };
    }

    // Runs on the fixed solution: only code that is still there can keep the removed package.
    private static async Task<IReadOnlyList<ProjectFileChange>> MigrateProjectFilesAsync(
        IProjectFileStore projectFiles,
        Solution solution,
        IFix fix,
        PackageMigration package,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProjectMigration> migrations = await ProjectPackages.PlanAsync(solution, fix, package, cancellationToken);
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
