using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public class FixReport
{
    public required Solution Solution { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }

    // Not required: a fix without a package migration, or a Fixer without a project file store, changes none.
    public IReadOnlyList<ProjectFileChange> ProjectFileChanges { get; init; } = [];
}
