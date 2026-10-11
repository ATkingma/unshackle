using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Interface.Output;

// Everything the commands show; mocked in the command tests.
public interface IConsoleView
{
    void ShowFixes(IEnumerable<IFix> fixes);

    void ShowSummary(IReadOnlyList<Finding> findings);

    void ShowDiff(string filePath, IReadOnlyList<DiffLine> lines);

    void ShowMessage(string message);

    void ShowError(string message);
}
