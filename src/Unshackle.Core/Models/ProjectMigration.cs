namespace Unshackle.Core.Models;

// One project whose project files should follow a package migration.
public class ProjectMigration
{
    public required string ProjectPath { get; init; }

    public required PackageMigration Package { get; init; }

    // True when code in the project still uses the removed package: it stays, only the added package is added.
    public required bool KeepRemovedPackage { get; init; }

    // Set when the project's own code no longer needs the removed package but a project that references it does (corpus Moq B2):
    // the path of that dependent, for the report.
    public string? KeptForDependent { get; init; }

    // <Using Include> items to swap, in the same pairs as the fix swaps top-level usings.
    public required IReadOnlyList<string> RemovedNamespaces { get; init; }

    public required IReadOnlyList<string> AddedNamespaces { get; init; }
}
