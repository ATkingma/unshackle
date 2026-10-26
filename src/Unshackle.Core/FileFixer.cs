using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// One file in, one FilePass out: the patterns and usings for one copy of a file against the fixed baseline (the seam for TAAK-298).
internal static class FileFixer
{
    // TAAK-198: each round of targeted imports can only add conflicts; real files settle in two or three.
    private const int MaxTargetedRounds = 8;

    // TAAK-223: true when the node is in code that every earlier pass saw as an inactive #if branch, so no earlier pass fixed it.
    // The first pass (no earlier ones) takes everything.
    internal static Func<SyntaxNode, bool> NewCode(SyntaxNode root, IReadOnlyList<ParseOptions> earlier)
    {
        if (earlier.Count == 0)
        {
            return _ => true;
        }

        string text = root.ToFullString();
        List<List<TextSpan>> inactive =
        [
            .. earlier.Select(options => CSharpSyntaxTree.ParseText(text, (CSharpParseOptions)options).GetRoot()
                .DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)).Select(t => t.FullSpan).ToList()),
        ];
        return node => inactive.All(spans => spans.Any(s => s.Contains(node.Span)));
    }

    // One pass over a file: the patterns, then the usings. A rewrite that calls a blocked name is Manual with the blocked reason;
    // targeted leaves the added namespaces out of the usings (the caller imports the classes that do not conflict, TAAK-198).
    // Updated is null when the usings were not touched.
    internal static async Task<FilePass> FixFileAsync(
        IFix fix,
        Solution original,
        Solution baseline,
        DocumentId documentId,
        HashSet<string> swappedTypes,
        HashSet<ProjectId> globalProjects,
        IReadOnlyDictionary<string, string> blocked,
        bool targeted,
        IReadOnlyList<ParseOptions> earlier,
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
            SyntaxNode newRoot = await ApplyPatternAsync(fix, pattern, filePath, root, model, document.Project.Solution, blocked, NewCode(root, earlier), findings, requiredNamespaces, cancellationToken);
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
        SyntaxNode currentRoot = (await current.GetSyntaxRootAsync(cancellationToken))!;
        Func<SyntaxNode, bool> isNew = NewCode(currentRoot, earlier);
        findings.AddRange(UsingDirectives.UnboundCalls(currentRoot, currentModel, fix.RemovedExtensionMethods, cancellationToken).Where(n => isNew(n))
            .Select(n => FindingFactory.CreateUsingsFinding(updated.FilePath ?? updated.Name, n.GetLocation().GetLineSpan().StartLinePosition.Line + 1, fix, PatternResult.Manual(
                $"{n.Identifier.ValueText}() binds to nothing here (the type it is called on is unknown, e.g. after a rewrite in a lambda earlier on), " +
                "so no pattern migrated it and the old using stays. Run the fix again once the project builds, or migrate it by hand."))));
        if (UsingDirectives.KeepsRemovedUsing(updatedRoot, currentModel.Compilation, fix.RemovedNamespaces))
        {
            // Line in the file as the patterns saw it, like their findings; 0 (whole file) when only the updated root has it.
            SyntaxTrivia disabled = (await current.GetSyntaxRootAsync(cancellationToken))!.DescendantTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.DisabledTextTrivia));
            int line = disabled.IsKind(SyntaxKind.DisabledTextTrivia) ? disabled.GetLocation().GetLineSpan().StartLinePosition.Line + 1 : 0;
            findings.Add(FindingFactory.CreateUsingsFinding(updated.FilePath ?? updated.Name, line, fix, PatternResult.Manual(
                $"{Fixer.InactiveBranchReason}, so the old using stays. Migrate that branch by hand.")));
        }

        return new FilePass { Working = working, Changed = changed, ImportsAdded = requiredNamespaces.Count > 0, Updated = updated };
    }

    // TAAK-198: a file whose rewrite conflicts with an own extension (Akka.TestKit's ShouldBe next to Shouldly's) is migrated
    // anyway. Instead of the added namespace it gets `using static` for each added class it calls, minus the classes that make a
    // call ambiguous; rewrites that would call a conflicting name (or a name of a left-out class) stay as they were, as Manual.
    // Each round may find a new conflict; the sets only grow. Null when it does not work out (the caller keeps the whole file).
    internal static async Task<Solution?> FixWithTargetedImportsAsync(
        IFix fix,
        Solution original,
        Solution baseline,
        Solution unimported,
        DocumentId documentId,
        HashSet<string> swappedTypes,
        HashSet<ProjectId> globalProjects,
        IReadOnlyList<ParseOptions> earlier,
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
            FilePass pass = await FixFileAsync(fix, original, baseline, documentId, swappedTypes, globalProjects, blocked, true, earlier, findings, cancellationToken);
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
    internal static Solution AddReferences(Solution solution, IReadOnlyList<MetadataReference> references, IReadOnlyList<string> namespaces)
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

    // Returns the same root instance when nothing was rewritten.
    internal static async Task<SyntaxNode> ApplyPatternAsync(
        IFix fix,
        IPattern pattern,
        string filePath,
        SyntaxNode root,
        SemanticModel model,
        Solution solution,
        IReadOnlyDictionary<string, string> blocked,
        Func<SyntaxNode, bool> isNew,
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
            nodes = [.. found.Where(isNew).OrderBy(n => n.SpanStart).ThenByDescending(n => n.Span.Length)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(FindingFactory.CreateFinding(filePath, 0, fix, pattern, FindingFactory.InternalError(pattern, ex)));
            return root;
        }

        // TAAK-274: code a partial plan keeps goes to no pattern (RunPartialAsync checks afterwards that it is still there as it was).
        if (root.ContainsAnnotations)
        {
            nodes.RemoveAll(n => n.AncestorsAndSelf().Any(a => a.HasAnnotations(PartialPlan.KeptAnnotationKind)));
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
                result = PatternResult.Manual($"{Fixer.NotRewrittenPrefix}{reason}");
            }

            (newRoot, PatternResult reported) = result switch
            {
                FixedResult fixedResult => Replace(newRoot, node, fixedResult, pattern),
                ManualResult or InfoResult => (newRoot, result),
                _ => throw new UnreachableException(),
            };

            if (reported is FixedResult applied)
            {
                changed = true;
                requiredNamespaces.UnionWith(applied.RequiredNamespaces);
            }

            int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(FindingFactory.CreateFinding(filePath, line, fix, result.PatternId ?? pattern.Id, reported));
        }

        return changed ? newRoot : root;
    }

    // The reason of the first call in the rewrite to a name that conflicts with an own extension (TAAK-198); null when there is none.
    internal static string? BlockedCall(PatternResult result, IReadOnlyDictionary<string, string> blocked) =>
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

    internal static async Task<PatternResult> RewriteAsync(IPattern pattern, SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        try
        {
            return pattern is ISolutionPattern solutionPattern
                ? await solutionPattern.RewriteAsync(node, model, solution, cancellationToken)
                : pattern.Rewrite(node, model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return FindingFactory.InternalError(pattern, ex);
        }
    }

    internal static (SyntaxNode Root, PatternResult Result) Replace(SyntaxNode root, SyntaxNode node, FixedResult result, IPattern pattern)
    {
        SyntaxNode? current = root.GetCurrentNode(node);
        if (current is null)
        {
            return (root, PatternResult.Manual("Overlaps an earlier finding of this pattern; run the fix again."));
        }

        // A directive inside the node that the rewrite drops would leave its partner behind (#if gone, #endif stays: CS1028, TAAK-281).
        // #region/#endregion may only go as a pair (TAAK-280 repairs AutoMapper profiles that way).
        int DroppedDirectives(Func<SyntaxTrivia, bool> kind) => InnerDirectives(current, kind) - result.NewNodes.Sum(n => InnerDirectives(n, kind));
        if (DroppedDirectives(t => !IsRegionDirective(t)) > 0
            || DroppedDirectives(t => t.IsKind(SyntaxKind.RegionDirectiveTrivia)) != DroppedDirectives(t => t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)))
        {
            return (root, PatternResult.Manual("Not rewritten: the code has #if/#else/#region directives inside it, which a rewrite would break apart. Migrate it by hand."));
        }

        try
        {
            return (ReplaceWithTrivia(root, current, result.NewNodes), result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (root, FindingFactory.InternalError(pattern, ex));
        }
    }

    // Directives and disabled text inside the node, not those in front of it (they stay with the node's position).
    internal static int InnerDirectives(SyntaxNode node, Func<SyntaxTrivia, bool> kind)
    {
        SyntaxToken first = node.GetFirstToken();
        return node.DescendantTrivia().Count(t => (t.IsDirective || t.IsKind(SyntaxKind.DisabledTextTrivia)) && kind(t) && !(t.Token == first && t.Span.End <= first.SpanStart));
    }

    internal static bool IsRegionDirective(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia);

    internal static SyntaxNode ReplaceWithTrivia(SyntaxNode root, SyntaxNode current, IReadOnlyList<SyntaxNode> newNodes)
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
}
