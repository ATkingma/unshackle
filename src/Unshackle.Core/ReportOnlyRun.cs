using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// A report-only fix (council round 8, S2): the patterns find and judge every node, nothing is rewritten, no using or package is
// touched. The first row is the "inventory only" header; a Fixed result a pattern still returns becomes Manual.
internal static class ReportOnlyRun
{
    internal const string InventoryOnlyNote = "Inventory only: this fix changes no code and no packages; every row is work to do by hand. ";

    internal const string RewriteNotAppliedReason = Fixer.NotRewrittenPrefix + "this fix is report-only, so it changes no code. Migrate this by hand.";

    internal static async Task<FixReport> RunAsync(IFix fix, IReportOnlyFix reportOnly, Solution solution, FixRequest request, CancellationToken cancellationToken)
    {
        List<Finding> findings =
        [
            FindingFactory.CreateFinding(solution.FilePath ?? ".", 0, fix, Fixer.InventoryOnlyPatternId, PatternResult.Note(InventoryOnlyNote + reportOnly.ReportOnlyReason)),
        ];

        // A multi-target project has one document per target framework: a file is reported once, from its first copy.
        List<IGrouping<string, DocumentId>> files =
        [
            .. solution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                .SelectMany(p => p.Documents)
                .GroupBy(d => d.FilePath ?? d.Id.Id.ToString(), d => d.Id, StringComparer.Ordinal),
        ];
        HashSet<string>? candidates = request.CheckEveryFile || !fix.FindsOnlyOldLibraryCode ? null : await OldLibraryFiles.FindAsync(solution, fix, files, cancellationToken);
        foreach (IGrouping<string, DocumentId> file in files.Where(f => candidates?.Contains(f.Key) != false))
        {
            Document document = solution.GetDocument(file.First())!;
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
            foreach (IPattern pattern in fix.Patterns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReportPatternAsync(fix, pattern, file.Key, root, model, solution, findings, cancellationToken);
            }
        }

        return new FixReport { Solution = solution, Findings = findings, ProjectFileChanges = [], InventoryOnly = true };
    }

    private static async Task ReportPatternAsync(
        IFix fix, IPattern pattern, string filePath, SyntaxNode root, SemanticModel model, Solution solution, List<Finding> findings, CancellationToken cancellationToken)
    {
        List<SyntaxNode> nodes;
        try
        {
            IEnumerable<SyntaxNode> found = pattern is ISolutionFinder finder
                ? await finder.FindAsync(root, model, solution, cancellationToken)
                : pattern.Find(root, model);
            nodes = [.. found.OrderBy(n => n.SpanStart).ThenByDescending(n => n.Span.Length)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            findings.Add(FindingFactory.CreateFinding(filePath, 0, fix, pattern, FindingFactory.InternalError(pattern, ex)));
            return;
        }

        foreach (SyntaxNode node in nodes)
        {
            PatternResult result = await FileFixer.RewriteAsync(pattern, node, model, solution, cancellationToken);
            PatternResult reported = result switch
            {
                FixedResult => PatternResult.Manual(RewriteNotAppliedReason),
                ManualResult or InfoResult => result,
                _ => throw new UnreachableException(),
            };
            int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(FindingFactory.CreateFinding(filePath, line, fix, pattern, reported));
        }
    }
}
