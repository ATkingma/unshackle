using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public class FixReport
{
    public required Solution Solution { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }

    // Not required: a fix without a package migration, or a Fixer without a project file store, changes none.
    public IReadOnlyList<ProjectFileChange> ProjectFileChanges { get; init; } = [];

    // True for a report-only fix (IReportOnlyFix, council round 8, S2): nothing was changed, the findings are an inventory.
    public bool InventoryOnly { get; init; }

    // The findings every count is made of: all rows except the header row of a report-only fix (council round 10, G1).
    public IEnumerable<Finding> CountedFindings => Findings.Where(IsCounted);

    // False only for the header row of a report-only fix (InfoKind.Header).
    public static bool IsCounted(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        return finding.Result is not InfoResult { Kind: InfoKind.Header };
    }
}
