using System.Globalization;
using Spectre.Console;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;

namespace Unshackle.Interface.Output;

public class SpectreConsoleView : IConsoleView
{
    public const string InventoryOnlyLine = "Inventory only: nothing was changed.";

    private readonly IAnsiConsole _out;
    private readonly IAnsiConsole _error;

    public SpectreConsoleView(IAnsiConsole output, IAnsiConsole error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        _out = output;
        _error = error;
    }

    public void ShowFixes(IEnumerable<IFix> fixes)
    {
        Table table = new Table().AddColumns("Fix", "Pattern", "Description");
        foreach (IFix fix in fixes)
        {
            foreach (IPattern pattern in fix.Patterns)
            {
                table.AddRow(new Text(fix.Id), new Text(pattern.Id), new Text(pattern.Description));
            }
        }

        _out.Write(table);
    }

    public void ShowSummary(IReadOnlyList<Finding> findings, IReadOnlyDictionary<string, string> projectOfFile)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(projectOfFile);
        // A report-only fix (council round 8 S2, round 10 G1): say so first; its header row is not a finding and counts nowhere.
        if (findings.Any(f => !FixReport.IsCounted(f)))
        {
            _out.WriteLine(InventoryOnlyLine);
            findings = [.. findings.Where(FixReport.IsCounted)];
        }

        Table table = new Table().AddColumns("Pattern", "Fixed", "Manual");
        foreach (IGrouping<string, Finding> group in findings.GroupBy(f => f.PatternId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            table.AddRow(
                new Text(group.Key),
                new Text(group.Count(f => f.Result is FixedResult).ToString(CultureInfo.InvariantCulture)),
                new Text(group.Count(f => f.Result is ManualResult).ToString(CultureInfo.InvariantCulture)));
        }

        _out.Write(table);
        List<IGrouping<string, Finding>> custom = [.. CustomAssertionPattern.Group(findings).OrderByDescending(g => g.Count())];
        if (custom.Count > 0)
        {
            _out.WriteLine("Custom assertions: rewrite each one to Shouldly once, its calls can stay.");
            foreach (IGrouping<string, Finding> group in custom)
            {
                _out.WriteLine(ConsoleText.Sanitize($"{group.Count(),6}x {group.Key}"));
            }
        }

        // A few Manual findings can hold back a whole migration (all or nothing): name them, so the user knows where to start.
        int blocked = findings.Count(f => f.Result is ManualResult m && m.Reason.StartsWith(Fixer.NotRewrittenPrefix, StringComparison.Ordinal));
        List<Finding> blockers = [.. findings.Where(f => f.Result is ManualResult m && !m.Reason.StartsWith(Fixer.NotRewrittenPrefix, StringComparison.Ordinal))];
        if (blocked > 0 && blockers.Count > 0)
        {
            _out.WriteLine(ConsoleText.Sanitize($"{blocked} findings wait on these {blockers.Count} manual ones; fix them first, then run again:"));
            // Council round 18, S2: every blocking project with every one of its blocking rows.
            foreach (IGrouping<string, Finding> project in blockers.GroupBy(f => projectOfFile.GetValueOrDefault(f.FilePath, f.FilePath), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                _out.WriteLine(ConsoleText.Sanitize($"  {project.Key} ({project.Count()} blocking):"));
                foreach (Finding blocker in project)
                {
                    _out.WriteLine(ConsoleText.Sanitize($"    {blocker.FilePath}:{blocker.Line} {blocker.PatternId}: {ReportRow.ReasonOf((ManualResult)blocker.Result)}"));
                }
            }
        }

        _out.WriteLine(
            $"{findings.Count(f => f.Result is FixedResult)} fixed ({findings.Count(f => f.Result is FixedResult { Warning: not null })} with a warning), " +
            $"{findings.Count(f => f.Result is ManualResult)} manual. " +
            "Use --report for every finding.");
    }

    public void ShowDiff(string filePath, IReadOnlyList<DiffLine> lines)
    {
        _out.Write(new Text($"--- {ConsoleText.Sanitize(filePath)}", new Style(decoration: Decoration.Bold)));
        _out.WriteLine();
        foreach (DiffLine line in lines)
        {
            (string sign, Color color) = line.Kind == DiffKind.Removed ? ("-", Color.Red) : ("+", Color.Green);
            _out.Write(new Text($"{sign}{line.LineNumber,5} | {ConsoleText.Sanitize(line.Text)}", new Style(color)));
            _out.WriteLine();
        }
    }

    public void ShowMessage(string message) => _out.WriteLine(ConsoleText.Sanitize(message));

    public void ShowError(string message) => _error.Write(new Text(ConsoleText.Sanitize(message) + Environment.NewLine, new Style(Color.Red)));
}
