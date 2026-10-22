# TAAK-316 — Report-only fix kind

**Status**: done (4/4)
**Created**: 2026-10-22
**Goal**: A fix can declare itself report-only: its patterns return only Manual/Info, the Fixer never touches packages for it, and the report header says "inventory only".

## Context for the next agent

- Decided by council round 8, decision S2 (`claude/docs/council.md`, 2026-10-22). Read that rule first; it is the spec.
- Core change: additive only. Do not change existing members of `IFix`, `IPattern` or `IPartialFix`; other agents build new fixes against them in parallel (Moq, Polly, EPPlus skeletons).
- Follows `claude/docs/code-standaarden.md` (class with `required init` for models, never `sealed`, `CancellationToken` required, GoF patterns).

## Design note (step 1)

- **Types** (all new, additive):
  - `IReportOnlyFix { string ReportOnlyReason { get; } }` in `Core/Abstractions`, implemented next to `IFix` (like `IPartialFix`).
  - `InfoResult(string Text) : PatternResult` plus factory `PatternResult.Note(string text)`. Not called `Info` because
    `FixedResult.Info` would hide it (CS0108).
  - `FixReport.InventoryOnly` (bool, default false); `Fixer.InventoryOnlyPatternId = "inventory-only"`.
- **Where the Fixer calls it**: first thing in `Fixer.FixAsync` after the fix lookup: `fix is IReportOnlyFix` → `ReportOnlyRun.RunAsync`.
  It runs Find + Rewrite per pattern on the first copy of each pre-filtered file and returns the input solution unchanged,
  no project-file changes, no vendoring, no usings. A `Fixed` result becomes Manual (`Not rewritten: ...report-only...`).
  Pattern exceptions become the usual `Internal error in ...` Manual row.
- **Report**: first row = Info row on the solution path, line 0, pattern `inventory-only`, "Inventory only: ... <reason>".
  `ReportRow` maps `InfoResult` to status `Info`. `MarkdownReportWriter` writes `**Inventory only**: ...` above the table when
  that row is present. The console summary (Interface, not owned) counts Info rows as neither fixed nor manual.
- `FileFixer` also accepts `InfoResult` from an ordinary fix (node left alone, row kept), instead of `UnreachableException`.

## Steps

- [x] 1. Design note in this file: types, where the Fixer calls it, how the report shows it
- [x] 2. Implement in Core (+ DataAccess for file I/O behind a Core interface)
- [x] 3. Unit tests (a fake report-only fix: no package edit, header text, not counted as Fixed)
- [x] 4. Docs (`docs/projectinfo/architectuur/core.md` section), build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after council round 8.
- 2026-10-22 (engine, claim #22): implemented; `Core/Engine/ReportOnlyFixTests` (6) and `DataAccess/Reports/InventoryOnlyReportTests` (3); full suite 1682 green.
- Open for the main session: the console summary in `SpectreConsoleView.ShowSummary` could print "Inventory only" when
  `FixReport.InventoryOnly` (Interface is not in this claim).

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core` (`Fixer.cs`, `ReportOnlyRun.cs`, `FileFixer.cs`, `Abstractions/IReportOnlyFix.cs`, `Models/InfoResult.cs`, `Models/PatternResult.cs`, `Models/FixReport.cs`), `src/Unshackle.DataAccess/Reports`, tests |
| **How**   | Engine extension |
| **Why**   | Needed for the MassTransit (TAAK-310) and IdentityServer4 (TAAK-381) detectors |

## See also

- `claude/docs/council.md` — round 8
- `claude/tasks/CLAIMS.md` — file claims
- `docs/projectinfo/architectuur/core.md` — section "Report-only fixes"
