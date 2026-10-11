using System.Globalization;
using Spectre.Console;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Interface.Output;

public class SpectreConsoleView : IConsoleView
{
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

    public void ShowSummary(IReadOnlyList<Finding> findings)
    {
        Table table = new Table().AddColumns("Pattern", "Fixed", "Manual");
        foreach (IGrouping<string, Finding> group in findings.GroupBy(f => f.PatternId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            table.AddRow(
                new Text(group.Key),
                new Text(group.Count(f => f.Result is FixedResult).ToString(CultureInfo.InvariantCulture)),
                new Text(group.Count(f => f.Result is ManualResult).ToString(CultureInfo.InvariantCulture)));
        }

        _out.Write(table);
        _out.WriteLine(
            $"{findings.Count(f => f.Result is FixedResult)} fixed, {findings.Count(f => f.Result is ManualResult)} manual. " +
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
