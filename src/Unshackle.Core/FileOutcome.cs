using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// What fixing one file (all its copies) gives the Fixer: its rows in order, its fixed text (null when it stays), and whether a later
// copy replaced an inactive-branch row, so that row goes from the rows before this file too (TAAK-298).
internal class FileOutcome
{
    public required IReadOnlyList<Finding> Findings { get; init; }

    public required SourceText? Text { get; init; }

    public required bool ClearsInactiveBranchRows { get; init; }
}
