# TAAK-017 — Migrate project files

**Status**: step 8/8 — checks, commit by main session
**Created**: 2026-10-11
**Goal**: `unshackle fix` also updates `.csproj`, `Directory.Build.props` and `Directory.Packages.props` (FluentAssertions out, Shouldly 4.3.0 in, `<Using>` converted), so the fixed solution builds without manual work.

## Context for the next agent

- Chain from TAAK-005: `Cli.cs` (composition root) → `ScanCommand.RunAsync` → `IFixer.FixAsync` → `FixReport`; `FixCommand` shows the
  diff (`LineDiff`) or writes via `ISolutionStore.SaveAsync` (`MsBuildSolutionStore`, documents only).
- `samples/SampleShop/SampleShop.Tests/SampleShop.Tests.csproj` has `<PackageReference Include="FluentAssertions" />` (CPM, without
  version) and `<Using Include="FluentAssertions" />`; `samples/Directory.Packages.props` has `<PackageVersion Include="FluentAssertions" Version="7.2.2" />`.
- After `fix` one Manual remains (`RepositoryTests.cs:16`, `fa.have-count` on a dictionary): that line still uses FA.
- Another agent is working on TAAK-018 in `src/Unshackle.Tests/` (patterns working together): do not touch their files;
  therefore not `ProofFix.cs` and `FixerTests.cs` either (extend the contract so they keep compiling unchanged).
- Do not modify `samples/`; the integration test works on a copy.

## Analysis

### Current situation

`IFix` only describes code (`Patterns`, `RemovedNamespaces`, `AddedNamespaces`). Nothing reads or writes project files.
TAAK-005 listed this as a follow-up task (risk table).

### Problem

- After `fix` the FA package and `<Using Include="FluentAssertions" />` are still there and Shouldly is missing: the code does not build.
- So the integration test cannot build and test the fixed sample; "fix works" is unproven.

### New situation

`fix` (and `scan`, `--dry-run`) changes each project that references FA assemblies: Shouldly added (PackageReference, with `Version` or via
`PackageVersion` under CPM), FA removed and `<Using>` converted, but only if no `.cs` file of that project still uses FA; otherwise
FA stays and a project-level Manual row is added. Report and diff show the project files (Line 0).

### Improvement

The fixed solution builds and the tests run without manual work (apart from the Manual findings); proven with `dotnet build` + `dotnet test` in the integration test.

## Approach

### What

1. Core: model `PackageMigration` (which package out, which in, version) as default member `IFix.Package` (null = nothing to project files).
   Model `ProjectMigration` (project, package, namespaces, `KeepRemovedPackage`) and `ProjectFileChange` (path, old and new text).
   Interface `IProjectFileStore` (`EditAsync` → changes without writing, `SaveAsync`). `FixReport.ProjectFileChanges`.
2. Core: `Fixer` determines per project whether FA is still used (semantically, like `UsingDirectives`), requests the changes and adds
   rows to the findings (Fixed per changed file, Manual per project that keeps FA).
3. DataAccess: `XmlProjectFileStore` (XDocument, `IFileSystem`) for csproj, nearest `Directory.Build.props` and `Directory.Packages.props`.
4. Interface: `Cli` passes the store to `Fixer` and `FixCommand`; `FixCommand` shows the diff and writes the project files.

### Where

`src/Unshackle.Core/Abstractions/{IFix,IProjectFileStore}.cs`, `src/Unshackle.Core/Models/{PackageMigration,ProjectMigration,ProjectFileChange,FixReport}.cs`,
`src/Unshackle.Core/{Fixer,ProjectPackages}.cs`, `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`,
`src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs`, `src/Unshackle.Interface/{Cli.cs,Commands/FixCommand.cs}`,
tests in `src/Unshackle.Tests/{DataAccess/XmlProjectFileStoreTests.cs,Core/FixerProjectFileTests.cs,Interface/Commands/CommandTests.cs,Integration/SampleShopTests.cs}`.

### How

KingmaDoc plan first; then the DataAccess store with `MockFileSystem` tests; then Core (Fixer + models) with an NSubstitute mock of
`IProjectFileStore`; then Interface; finally the integration test (fix → manual handling of Manual → fix → build + test on the copy).

### Why this approach

Deciding "is FA still used?" requires the SemanticModel → Core. Reading/writing XML is I/O → DataAccess behind a Core interface.
One batch call (`EditAsync` with all projects) because `Directory.*.props` is shared: FA may only be removed there if no project keeps it.

## Steps

- [x] 1. KingmaDoc plan `docs/projectinfo/features/projectbestanden-migreren-plan.md`, check + approve
- [x] 2. Core contract: models, `IProjectFileStore`, `IFix.Package`, `FluentAssertionsFix.Package`
- [x] 3. `XmlProjectFileStore` + unit tests (CPM, non-CPM, Using, props, FA stays)
- [x] 4. `Fixer`: determine projects, call store, findings + unit tests with NSubstitute
- [x] 5. `Cli` + `FixCommand` (diff and writing) + command tests
- [x] 6. Integration test: fix on a SampleShop copy, `dotnet build` + `dotnet test`, no FA in csproj/props
- [x] 7. KingmaDoc verify, `core.md`, `modellen/klassen.md` (render), README Documents, INDEX
- [x] 8. Three checks green (`claude/docs/agent-werkwijze.md` §6); commit/push by the main session

## Risks & edge cases

| Risk                                                          | Mitigation                                                                                               |
| ------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Removing FA while code still uses it → build broken           | Only remove if no `.cs` in the project binds FA symbols; otherwise Manual at project level               |
| Shared `Directory.*.props`: one project keeps FA              | FA stays in a shared file if one project in the batch keeps it                                           |
| XML formatting/comments get lost                              | `XDocument` with `PreserveWhitespace`; text outside `<Project>` kept literally; CRLF and BOM preserved    |
| Projects outside the solution share `Directory.Packages.props` | Known limit, in Choices; only projects in the solution count                                            |
| File changed between reading and writing                      | `SaveAsync` compares the current text with `OldText` and refuses on a difference                        |

## Changed files

See `files_expected` in the plan; additionally `src/Unshackle.DataAccess/Projects/ProjectFile.cs`. Docs: `projectbestanden-migreren-verify.md`, `architectuur/core.md`, `modellen/klassen.md` (+ `img/klassen-2.*`), README, INDEX.

## Choices

- Store tests in `DataAccess/Projects/` (like `Reports/`).
- Integration test skips `obj/` in the FA check: restore files from before the fix.
- Projects outside the solution that share `Directory.Packages.props` are not counted (known limit).

## See also

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `claude/tasks/archief/TAAK-005-end-to-end-keten.md` — CLI, `MsBuildSolutionStore`, follow-up task project files
