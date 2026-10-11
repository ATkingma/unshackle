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

    public int Execute()
    {
        _view.ShowFixes(_fixes);
        return ExitCodes.Ok;
    }
}
