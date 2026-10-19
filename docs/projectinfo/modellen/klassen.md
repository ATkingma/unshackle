# Classes: contracts and models of Unshackle.Core

**Figure 1.** Contracts (`Abstractions/`) and models (`Models/`) around `Fixer`. Omitted: `FluentAssertionsFix` and the patterns (see [patronen.md](patronen.md)) and `UsingDirectives`.

<!-- kingmadoc:diagram img/klassen-1.d2 -->
![Classes: contracts and models of Unshackle.Core](img/klassen-1.png)

| Element | Path | Role |
| ------- | ---- | ---- |
| `IFixer` / `Fixer` | `Abstractions/IFixer.cs`, `Fixer.cs` | Single entry point (Facade): runs one fix on a `Solution` |
| `IFix` | `Abstractions/IFix.cs` | One migration (Strategy): patterns plus usings to add and remove |
| `IPattern` | `Abstractions/IPattern.cs` | Finds (`Find`) and rewrites (`Rewrite`) one kind of code; never throws |
| `FixRequest` | `Models/FixRequest.cs` | Which fix (`FixId`) |
| `FixReport` | `Models/FixReport.cs` | New `Solution` and all findings |
| `Finding` | `Models/Finding.cs` | One found node: file, line (0 = whole file), fix, pattern, result |
| `PatternResult` | `Models/PatternResult.cs` | Factory methods `Fixed` (one or more nodes) and `Manual` |
| `FixedResult` | `Models/FixedResult.cs` | Replacement nodes and required namespaces |
| `ManualResult` | `Models/ManualResult.cs` | Reason why manual work is needed; `Kept` (`KeptReason`, `Models/KeptReason.cs`) marks a finding that stays on the old library while the rest is migrated (the report prints the prefix) |

**Figure 2.** Contracts that Core defines for I/O (`Abstractions/`) and their implementations in `Unshackle.DataAccess`.

<!-- kingmadoc:diagram img/klassen-2.d2 -->
![Classes: contracts and models of Unshackle.Core](img/klassen-2.png)

| Element | Path | Role |
| ------- | ---- | ---- |
| `ISolutionStore` / `MsBuildSolutionStore` | `Abstractions/ISolutionStore.cs`, `DataAccess/Solutions/` | Load the solution and save changes through a single `MSBuildWorkspace` |
| `SolutionLoadException` | `Abstractions/SolutionLoadException.cs` | MSBuild reported load errors (`Errors`); the CLI turns it into exit code 1 |
| `IReportWriter` / `Csv`-, `Json`-, `MarkdownReportWriter` | `Abstractions/IReportWriter.cs`, `DataAccess/Reports/` | One report format per extension (Strategy) |
| `ReportRow` | `DataAccess/Reports/ReportRow.cs` | `Finding` → columns `File,Line,Fix,Pattern,Status,Reason` |
| `IProjectFileStore` / `XmlProjectFileStore` | `Abstractions/IProjectFileStore.cs`, `DataAccess/Projects/` | Edit project files (`EditAsync` → `ProjectFileChange`, without writing) and save them (`SaveAsync`) |
| `ProjectMigration` / `PackageMigration` / `ProjectFileChange` | `Models/` | Per project: which package goes/comes, does the project keep the old package; result: old and new text per file |
| `ISourceControl` / `GitSourceControl` | `Abstractions/ISourceControl.cs`, `DataAccess/SourceControl/` | Does the working tree have uncommitted changes? (`fix` then refuses without `--force`) |

## See also

- [fix-flow.md](fix-flow.md) — how `FixAsync` uses these classes
- [../architectuur/core.md](../architectuur/core.md) — design and choices
- [../README.md](../README.md) — C4 overview
