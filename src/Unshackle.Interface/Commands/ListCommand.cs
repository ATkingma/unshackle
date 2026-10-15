using System.Globalization;
using Unshackle.Core.Abstractions;
using Unshackle.Interface.Output;

namespace Unshackle.Interface.Commands;

public class ListCommand
{
    private readonly IReadOnlyList<IFix> _fixes;
    private readonly IConsoleView _view;

    public ListCommand(IEnumerable<IFix> fixes, IConsoleView view)
    {
        ArgumentNullException.ThrowIfNull(fixes);
        ArgumentNullException.ThrowIfNull(view);
        _fixes = [.. fixes];
        _view = view;
    }

    public int Execute(bool markdown)
    {
        if (markdown)
        {
            // ShowMessage replaces control characters (a line break too), so one line at a time.
            foreach (string line in ToMarkdown(_fixes).Split('\n'))
            {
                _view.ShowMessage(line);
            }
        }
        else
        {
            _view.ShowFixes(_fixes);
        }

        return ExitCodes.Ok;
    }

    public static string ToMarkdown(IEnumerable<IFix> fixes)
    {
        ArgumentNullException.ThrowIfNull(fixes);
        var text = new System.Text.StringBuilder();
        foreach (IFix fix in fixes)
        {
            text.Append(CultureInfo.InvariantCulture, $"## {fix.Id}\n\n| Pattern | What it does |\n| --- | --- |\n");
            foreach (IPattern pattern in fix.Patterns)
            {
                text.Append(CultureInfo.InvariantCulture, $"| `{pattern.Id}` | {pattern.Description.Replace("|", "\\|", StringComparison.Ordinal)} |\n");
            }

            text.Append('\n');
        }

        return text.ToString().TrimEnd('\n');
    }
}
