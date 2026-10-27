using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Interface.Output;

// Everything the commands show; mocked in the command tests.
public interface IConsoleView
{
    void ShowFixes(IEnumerable<IFix> fixes);

    void ShowSummary(IReadOnlyList<Finding> findings);

    // Council round 18, X1/S2: the projects that held back an all-or-nothing fix, with their reasons; nothing when the list is empty.
    void ShowBlockingProjects(IReadOnlyList<BlockingProject> projects);

    void ShowDiff(string filePath, IReadOnlyList<DiffLine> lines);

    void ShowMessage(string message);

    void ShowError(string message);
}
