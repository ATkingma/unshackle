# TAAK-005 — End-to-end chain: loading, CLI, report, first integration test

**Status**: ✅ Done — 2026-10-11 (commit d5b8778)
**Created**: 2026-10-10
**Goal**: `unshackle scan|fix|list` works on a real solution (`samples/SampleShop`), with a report and `--dry-run` diff, proven by an integration test.

## Context for the next agent

- Progress 2026-10-10: ~15–20% of the roadmap. Foundation (TAAK-001) and 4 FA patterns (TAAK-002..004) are done,
  but the tool is **not usable**: `src/Unshackle.Interface/Program.cs` is `return 0;`, `src/Unshackle.DataAccess/`
  only has empty folders `Solutions/` and `Reports/`, `src/Unshackle.Tests/Integration/` is empty.
- Existing contract: `IFixer.FixAsync(Solution, FixRequest, CancellationToken)` → `FixReport { Solution, Findings }`;
  `Finding { FilePath, Line, FixId, PatternId, Result }`; `Fixer(IEnumerable<IFix>)`. Contracts in `Abstractions/`
  may be **extended** with new interfaces (`ISolutionStore`, `IReportWriter`); do not change existing ones.
- Packages are already in place: DataAccess `Microsoft.CodeAnalysis.Workspaces.MSBuild`, `System.IO.Abstractions`, `CsvHelper`;
  Interface `System.CommandLine`, `Spectre.Console`. No `Microsoft.Build.Locator` (Roslyn 5.x has its own BuildHost).
- The pattern loop is paused (stopped 2026-10-10 23:50); rows 4–14 in `claude/tasks/patroon-queue.md` are still open.
  This task comes before the loop: without the chain you cannot see patterns work on real code.

- 2026-10-11: steps 1–7 done (subagent). Next: step 8 — three checks on the commit itself, then commit + push (main session). Open: migrating project files (follow-up task); CI run of the integration test not yet seen.
- 2026-10-11: taken over by a subagent (Claude Opus 5.5) from the main session; steps 1–7, step 8 is done by the main session.

## Analysis

### Current situation

Patterns only work in proofs (in-memory `AdhocWorkspace` via `src/Unshackle.Tests/Helpers/TestSolution.cs`).
There is no way to load a `.sln/.slnx`, write changes back, create a report or start the tool.

### Problem

- Nobody can run Unshackle; roadmap step 1 (scan + report) is not finished.
- Whether patterns work together on real code (MSBuild, global usings from csproj, multiple projects) is unproven.

### New situation

`unshackle scan <sln> --fix fluentassertions [--report r.csv]`, `unshackle fix <sln> --fix … [--dry-run]` and
`unshackle list` work. An integration test runs the chain on a copy of `samples/SampleShop`.

### Improvement

First usable version; every following pattern can immediately be seen on real code (scan on SampleShop).

## Approach

### What

1. `ISolutionStore` (Core) → `MsBuildSolutionStore` (DataAccess): `LoadAsync(path, ct)` and `SaveAsync(Solution, ct)`
   via one `MSBuildWorkspace` (`TryApplyChanges`); return load errors (`workspace.Diagnostics`/`WorkspaceFailed`).
2. `IReportWriter` (Core) → `CsvReportWriter`, `JsonReportWriter`, `MarkdownReportWriter` (DataAccess, `IFileSystem`).
   Columns: `File,Line,Fix,Pattern,Status,Reason` (see `docs/projectinfo/architectuur/rapportage.md`).
3. Commands (Interface, `Commands/`): `scan`, `fix` (`--dry-run` shows a diff per file), `list` (fixes + pattern Id + Description).
   Console output in `Output/` via Spectre (table with Fixed/Manual per pattern).
4. `Program.cs`: composition root by hand (`new Fixer([new FluentAssertionsFix()])` etc.), no DI container.
5. Integration test in `src/Unshackle.Tests/Integration/`: copy `samples/SampleShop` to a temp folder, `scan` + `fix`,
   check the report (Fixed/Manual counts) and a few rewritten lines.

### Where

`src/Unshackle.Core/Abstractions/ISolutionStore.cs`, `IReportWriter.cs`; `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`;
`src/Unshackle.DataAccess/Reports/{Csv,Json,Markdown}ReportWriter.cs`; `src/Unshackle.Interface/Commands/{Scan,Fix,List}Command.cs`;
`src/Unshackle.Interface/Output/*`; `src/Unshackle.Interface/Program.cs`; `src/Unshackle.Tests/Integration/SampleShopTests.cs`.

### How

First DataAccess + unit tests (`MockFileSystem` for writers), then commands with mocked `IFixer`/`IReportWriter`
(NSubstitute, see `docs/projectinfo/architectuur/testen.md`), then `Program.cs`, and the integration test last.
KingmaDoc plan up front (`docs/projectinfo/patterns/` is for patterns; this is a feature → plan in `docs/projectinfo/features/`
with `kingmadoc plan -o`), verify afterwards, and update the models (Container/Component Interface + DataAccess).

### Why this approach

Manual composition root: three classes, a DI container is unnecessary. Diff: first determine it simply per line ourselves
(old vs new text of changed documents); only add a package like DiffPlex if that proves too limited
(MIT, no licence check — but check with the user first).

## Steps

- [x] 1. KingmaDoc plan `docs/projectinfo/features/end-to-end-keten-plan.md` (requirements per command), check + approve
- [x] 2. `ISolutionStore` + `MsBuildSolutionStore` (load, save, load errors) + test on `samples/SampleShop`
- [x] 3. `IReportWriter` + CSV/JSON/Markdown writers + unit tests (`MockFileSystem`)
- [x] 4. Commands `list`, `scan`, `fix` (+ `--dry-run` diff) + unit tests with mocks
- [x] 5. `Program.cs` composition root; manually `dotnet run --project src/Unshackle.Interface -- scan samples/SampleShop/SampleShop.slnx --fix fluentassertions`
- [x] 6. Integration test on a copy of SampleShop (trait `Category=Integration`, filterable separately)
- [x] 7. KingmaDoc verify, update models (Models brief in `claude/docs/patroon-loop.md`), docs `rapportage.md` + `core.md`
- [x] 8. Three checks green (`claude/docs/agent-werkwijze.md` §6), commit + push

## Risks & edge cases

| Risk                                                                                                                                                   | Mitigation                                                                                                                            |
| ------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| MSBuildWorkspace does not load (SDK version, BuildHost) in CI or locally                                                                               | Show load errors and return them as exit code ≠ 0; the integration test runs in CI → immediately visible                              |
| SampleShop uses `<Using Include="FluentAssertions" />` and central package management: after `fix` the FA package and global using are still in csproj/props | Out of scope: report as Manual at project level; follow-up task "migrate project files" (swap package, adjust `<Using>`)        |
| `fix` overwrites user code                                                                                                                             | `scan` first by default; `fix` only on a clean git working tree or with `--force` (decide in step 1)                                  |
| Integration test modifies `samples/`                                                                                                                   | Always on a copy in a temp folder                                                                                                     |
| Concurrent pattern agents                                                                                                                              | Loop is paused; only restart after this task                                                                                          |

## Changed files

- Core: `Abstractions/{ISolutionStore,IReportWriter,ISourceControl,SolutionLoadException}.cs` (new; existing contracts unchanged)
- DataAccess: `Solutions/MsBuildSolutionStore.cs`, `Reports/{ReportRow,CsvReportWriter,JsonReportWriter,MarkdownReportWriter}.cs`,
  `SourceControl/GitSourceControl.cs` (`.gitkeep` in `Solutions/`, `Reports/` removed)
- Interface: `Cli.cs`, `Program.cs`, `Commands/{ScanCommand,FixCommand,ListCommand,ScanResult,ExitCodes,CommandException}.cs`,
  `Output/{IConsoleView,SpectreConsoleView,LineDiff,DiffLine,DiffKind}.cs`, `Unshackle.Interface.csproj` (`<Version>0.1.0-preview</Version>`)
- Tests: `DataAccess/{MsBuildSolutionStoreTests,GitSourceControlTests}.cs`, `DataAccess/Reports/ReportWriterTests.cs`,
  `Interface/CliTests.cs`, `Interface/Commands/CommandTests.cs`, `Interface/Output/LineDiffTests.cs`,
  `Integration/SampleShopTests.cs`, `Helpers/{TempDirectory,RepositoryRoot}.cs` (`.gitkeep`s removed)
- `.editorconfig` (CA1716 off for Interface code and tests)
- Docs: `docs/projectinfo/features/end-to-end-keten-{plan,verify}.md`, `docs/projectinfo/README.md` (Documents, C4 1/2, new
  Figure 4), `docs/projectinfo/img/README-2.*`, `README-4.*`, `docs/projectinfo/modellen/klassen.md` + `img/klassen-2.*`,
  `architectuur/{core,rapportage}.md`, `claude/docs/INDEX.md`, `README.md` (section Usage)

## Choices

| Choice | Reason |
| --- | --- |
| `fix` refuses on a git working tree with uncommitted (including untracked) changes, unless `--force`; `--dry-run` always allowed. **Provisional** | Safe default: only on a clean working tree is `git checkout .` a complete undo. Whole working tree instead of just the solution folder: projects can live outside that folder |
| No git or no repo → counts as clean | Otherwise the tool does not work on folders without git (nor in the integration test's temp folder); the user still sees the scan |
| Exit codes 0/1/2; dirty working tree = 1 | 2 is reserved for "invoked incorrectly"; a dirty working tree is a state, not an argument error. Manual is an outcome → 0 |
| Parse errors → 2 via `ParseResult.Errors` after `InvokeAsync` | System.CommandLine returns 1 by default; this keeps its own help/error output intact |
| `--fix` with `AcceptOnlyFromAmong(fix-ids)` | An unknown fix fails immediately as an argument error (2), before the slow loading |
| Load errors = only `WorkspaceDiagnosticKind.Failure` → `SolutionLoadException`; warnings ignored | MSBuild often emits harmless warnings; on a failure the SemanticModel is wrong and rewrites are unreliable |
| `MsBuildSolutionStore` holds one `MSBuildWorkspace` (IDisposable) | `TryApplyChanges` only works on the workspace that loaded the solution |
| Composition root in `Cli.cs`, `Program.cs` is one line | Tests (`CliTests`, integration test) run the real CLI in-process, without `InternalsVisibleTo` or an extra process |
| `FixCommand` reuses `ScanCommand.RunAsync` | Fix = scan + write; one place for loading, report and error translation |
| `IReportWriter.Extension` instead of a separate factory | Choosing by extension is one `FirstOrDefault`; a factory class adds nothing |
| Report paths relative to the solution folder, with `/` | Readable and the same on Windows and Linux |
| Own LCS line diff (`LineDiff`), common prefix/suffix removed first | No new package needed; rewrites are local, so the LCS table stays small |
| Spectre width 1000 for redirected output | Otherwise Spectre wraps paths and diff lines at 80 columns (pipes, PowerShell capture) |
| Errors to stderr (separate `IAnsiConsole`) | Scripts can separate stdout (table/diff) and errors |
| CA1716 off for Interface | Project name `Unshackle.Interface` is fixed in the architecture; only a VB keyword, exe without consumers |
| Integration test copies `samples/` without `bin/obj` to a temp folder with spaces and runs `dotnet restore` | `obj/` contains absolute paths; restore makes the copy truly loadable, also in CI; spaces prove REQ-10 |
| Integration test checks fixed rows (`fa.should-be` line 24, `fa.have-count` Manual) + "≥ 20 Fixed" instead of exact totals | Parallel pattern agents add patterns; exact totals would break with every new pattern |
| Version `0.1.0-preview` | Tool is installable but not finished yet (project files not migrated) |
| No new NuGet packages | Everything could be done with what was already there (System.CommandLine, Spectre, CsvHelper, System.IO.Abstractions, MSBuild workspaces) |

## See also

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `docs/projectinfo/architectuur/projecten.md` — dependencies Interface → Core ← DataAccess
- `docs/projectinfo/architectuur/rapportage.md` — report format
- `claude/tasks/archief/TAAK-001-fundament.md` — contract and choices
