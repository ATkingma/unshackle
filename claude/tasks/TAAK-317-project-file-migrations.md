# TAAK-317 — Project-file migrations (IProjectFileMigration)

**Status**: done (4/4)
**Created**: 2026-10-22
**Goal**: Fixes can declare exact, typed project/config-file edits (package rename/remove/add, one MSBuild property, known config file rename/transform), applied by the Fixer after the document pass and only when the project's C# part has 0 blocking Manual rows.

## Context for the next agent

- Decided by council round 8, decision S3 (`claude/docs/council.md`, 2026-10-22). Read that rule first; it is the spec.
- Core change: additive only. Do not change existing members of `IFix`, `IPattern` or `IPartialFix`; other agents build new fixes against them in parallel (Moq, Polly, EPPlus skeletons).
- Follows `claude/docs/code-standaarden.md` (class with `required init` for models, never `sealed`, `CancellationToken` required, GoF patterns).

## Design note (step 1)

- **Types** (all new): `IProjectFileMigration.EditsFor(Project)` (implemented next to `IFix`, like `IPartialFix`);
  `IProjectFileEditor.ApplyAsync(projectPath, edits, earlier, ct)` (Core interface, implemented by `XmlProjectFileStore`);
  closed model set `ProjectFileEdit` → `PackageRenameEdit`, `PackageRemoveEdit`, `PackageAddEdit`, `PropertyEdit`,
  `ConfigFileRenameEdit`, `ConfigFileTransformEdit` (+ `TextReplacement`); `ProjectFileEditOutcome`; `ProjectFileChange.Deletes`.
- **Where the Fixer calls it**: end of `Fixer.RunAsync`, after vendoring and the package migration, when the fix implements
  `IProjectFileMigration` and the store implements `IProjectFileEditor`. No Cli change needed. Per project file (multi-target
  grouped): blocking = Manual rows with `Kept` None in that project's documents → one Manual row, no edits.
- **Report**: one `project-file` row per edit on file:line (Fixed with Info = `Describe()`, or Manual with reason);
  `OutputType`/`IsTestProject` always warn. Changes go into `FixReport.ProjectFileChanges`, replacing an earlier change of the same file.
- **Conservative choices**: only the project file and files next to it are edited; a package declared in a shared
  `Directory.*.props` is Manual; under CPM a renamed package's old `PackageVersion` stays (other projects may use it).

## Steps

- [x] 1. Design note in this file: types, where the Fixer calls it, how the report shows it
- [x] 2. Implement in Core (+ DataAccess for file I/O behind a Core interface)
- [x] 3. Unit tests with a before/after snapshot proof per edit kind; OutputType/IsTestProject edits warn
- [x] 4. Docs (`docs/projectinfo/architectuur/core.md` section), build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after council round 8.
- 2026-10-22 (engine, claim #22): implemented; `DataAccess/Projects/ProjectFileEditTests` (16, snapshot proofs) and
  `Core/Engine/ProjectFileMigrationTests` (10); full suite 1708 green.
- Open for the main session: `FixCommand` dry-run shows a `Deletes` change as a diff with every line removed; it could say "deleted".

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core` (`Fixer.cs`, `ProjectFileMigrations.cs`, `Abstractions/IProjectFileMigration.cs`, `Abstractions/IProjectFileEditor.cs`, `Models/*Edit.cs`, `Models/ProjectFileEditOutcome.cs`, `Models/TextReplacement.cs`, `Models/ProjectFileChange.cs`), `src/Unshackle.DataAccess/Projects` (`XmlProjectFileStore.Edits.cs`, `EditRun.cs`, `SaveAsync` deletes), tests |
| **How**   | Engine extension |
| **Why**   | Needed for SpecFlow → Reqnroll and for CPM pins (Polly P6); patterns never write files |

## See also

- `claude/docs/council.md` — round 8
- `claude/tasks/CLAIMS.md` — file claims
- `docs/projectinfo/architectuur/core.md` — section "Project-file migrations"
