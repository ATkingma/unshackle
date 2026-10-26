using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// TAAK-274: the partial run of an all-or-nothing fix: the code tied to the Manual findings stays on the old library, the rest moves.
internal static class PartialRun
{
    // TAAK-274: each partial round can only keep more; a plan that does not settle in a few rounds falls back to all-or-nothing.
    private const int MaxPartialRounds = 4;

    // Plans a partial run from the Manual findings, runs it, and plans again with every new blocking Manual finding as an extra
    // root. Null when the fix finds no safe plan or the plan does not settle.
    internal static async Task<FixReport?> RunPartialAsync(
        IPartialFix partial,
        Solution solution,
        FixReport firstRun,
        Func<Solution, IReadOnlyList<Finding>, CancellationToken, Task<FixReport>> run,
        CancellationToken cancellationToken)
    {
        List<Finding> roots = [];
        // Rows that only say the old library stays somewhere are no roots; in a partial run a kept global using is (it would clash).
        foreach (Finding finding in firstRun.Findings.Where(f => f.Result is ManualResult manual && !manual.Reason.StartsWith(Fixer.NotRewrittenPrefix, StringComparison.Ordinal)
            && !IsKeptPackageRow(f) && !(f.PatternId == Fixer.UsingsPatternId && manual.Reason.StartsWith(Fixer.KeptGlobalUsingReason, StringComparison.Ordinal))))
        {
            roots.Add(finding.PatternId == Fixer.UnmatchedPatternId ? await InOriginalAsync(finding, solution, firstRun.Solution, cancellationToken) : finding);
        }

        for (int round = 0; round < MaxPartialRounds; round++)
        {
            if (await partial.PlanPartialAsync(solution, roots, cancellationToken) is not PartialPlan plan)
            {
                return null;
            }

            FixReport report = await run(plan.Solution, plan.Findings, cancellationToken);
            if (!await KeepsKeptCodeAsync(plan.Solution, report.Solution, cancellationToken))
            {
                return null;
            }

            // A root the run reports again (a project-level blocker row, council round 10 G4) is planned, not new.
            List<Finding> blocking = [.. report.Findings.Where(f => f.Result is ManualResult && !plan.Findings.Contains(f) && !IsKeptPackageRow(f) && !roots.Any(r => SameRow(r, f)))];
            if (blocking.Count == 0)
            {
                return report;
            }

            int known = roots.Count;
            roots.AddRange(blocking.Where(b => !roots.Any(r => r.FilePath == b.FilePath && r.Line == b.Line)));
            if (roots.Count == known)
            {
                return null;
            }
        }

        return null;
    }

    // Every outermost kept node of the plan is still in the fixed file, in order and with the same text: no pattern or using step
    // rewrote code that stays on the old library.
    private static async Task<bool> KeepsKeptCodeAsync(Solution planned, Solution fixedSolution, CancellationToken cancellationToken)
    {
        foreach (Document document in planned.Projects.SelectMany(p => p.Documents))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            if (!root.ContainsAnnotations || fixedSolution.GetDocument(document.Id) is not Document fixedDocument)
            {
                continue;
            }

            string text = (await fixedDocument.GetTextAsync(cancellationToken)).ToString();
            int position = 0;
            foreach (SyntaxNode kept in root.GetAnnotatedNodes(PartialPlan.KeptAnnotationKind)
                .Where(n => !n.Ancestors().Any(a => a.HasAnnotations(PartialPlan.KeptAnnotationKind))).OrderBy(n => n.SpanStart))
            {
                int found = text.IndexOf(kept.ToString(), position, StringComparison.Ordinal);
                if (found < 0)
                {
                    return false;
                }

                position = found + kept.Span.Length;
            }
        }

        return true;
    }

    // An unmatched-use row has its line in the fixed file (added usings move it down): the plan needs the line in the original. The
    // code it points at was not rewritten, so the same line is in the original, near the same number.
    private static async Task<Finding> InOriginalAsync(Finding finding, Solution original, Solution fixedSolution, CancellationToken cancellationToken)
    {
        if (finding.Line < 1 || original.GetDocumentIdsWithFilePath(finding.FilePath).FirstOrDefault() is not DocumentId id
            || fixedSolution.GetDocument(id) is not Document fixedDocument)
        {
            return finding;
        }

        TextLineCollection before = (await original.GetDocument(id)!.GetTextAsync(cancellationToken)).Lines;
        TextLineCollection after = (await fixedDocument.GetTextAsync(cancellationToken)).Lines;
        if (finding.Line > after.Count)
        {
            return finding;
        }

        string line = after[finding.Line - 1].ToString().Trim();
        for (int distance = 0; distance < Math.Max(before.Count, after.Count); distance++)
        {
            foreach (int candidate in (int[])[finding.Line - 1 - distance, finding.Line - 1 + distance])
            {
                if (candidate >= 0 && candidate < before.Count && before[candidate].ToString().Trim() == line)
                {
                    return new Finding { FilePath = finding.FilePath, Line = candidate + 1, FixId = finding.FixId, PatternId = finding.PatternId, Result = finding.Result };
                }
            }
        }

        return finding;
    }

    private static bool SameRow(Finding a, Finding b) =>
        a.FilePath == b.FilePath && a.Line == b.Line && a.PatternId == b.PatternId && a.Result == b.Result;

    // The project-file rows that kept code causes: the old package stays next to the new one.
    private static bool IsKeptPackageRow(Finding finding) =>
        finding.PatternId == Fixer.ProjectFilePatternId && finding.Result is ManualResult { Reason: var reason }
        && (reason.Contains(Fixer.KeptPackageReason, StringComparison.Ordinal) || reason.Contains(Fixer.KeptSharedPackageReason, StringComparison.Ordinal));
}
