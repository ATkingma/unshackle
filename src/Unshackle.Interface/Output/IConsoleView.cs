using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Interface.Output;

// Everything the commands show; mocked in the command tests.
public interface IConsoleView
{
    void ShowFixes(IEnumerable<IFix> fixes);

    // projectOfFile: the project path of each source file in the solution (council round 18, S2: blocking rows are grouped per project);
    // a path that is not in it (a project file, a config file) is its own group.
    void ShowSummary(IReadOnlyList<Finding> findings, IReadOnlyDictionary<string, string> projectOfFile);

    void ShowDiff(string filePath, IReadOnlyList<DiffLine> lines);

    void ShowMessage(string message);

    void ShowError(string message);
}
