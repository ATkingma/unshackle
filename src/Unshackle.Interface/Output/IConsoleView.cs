using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Interface.Output;

// Everything the commands show; mocked in the command tests.
public interface IConsoleView
{
    void ShowFixes(IEnumerable<IFix> fixes);

    // projectOfFile: the project path of each source file in the solution (council round 18, S2: blocking rows are grouped per project);
    // a path that is not in it (a project file, a config file) is its own group.
    // listBlockers false: the blocking rows are left out, because ShowBlockingProjects already names them (one list, not two).
    void ShowSummary(IReadOnlyList<Finding> findings, IReadOnlyDictionary<string, string> projectOfFile, bool listBlockers);

    // Council round 18, X1/S2: the projects that held back an all-or-nothing fix, with their reasons; nothing when the list is empty.
    void ShowBlockingProjects(IReadOnlyList<BlockingProject> projects);

    void ShowDiff(string filePath, IReadOnlyList<DiffLine> lines);

    void ShowMessage(string message);

    void ShowError(string message);
}
