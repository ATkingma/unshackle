# TAAK-022 — Scan/fix counts findings per target framework

**Status**: done
**Created**: 2026-10-11
**Found in**: TAAK-019 step 1, case microsoft/PowerApps-Tooling @ 630e6973

## Problem

For multi-target projects (`net48;net8.0;net10.0`) MSBuildWorkspace loads each project per TFM. As a result scan reports every finding 3x: table "778 fixed, 162 manual", in reality 262 Fixed / 53 Manual unique lines. `fix` reports "64 file(s) written" while 27 files change. The report (`--report`) contains the same line 3x.

Not a bug in a pattern, but misleading numbers (README/launch!) and possibly duplicate writes.

## Approach (proposal)

Deduplicate documents by file path (process one TFM per file, or deduplicate findings on (path, line, pattern)) in the Fixer/workspace layer. Test with a multi-target sample.

## Steps

- [x] Test: multi-target project reports each finding once
- [x] Deduplicate + write files once
- [ ] Case again: numbers match the report (not run; integration test `ScanFix_MultiTargetProject_ReportsAndWritesEachFileOnce` covers it)

## Result

- `Fixer` groups documents by file path: patterns run on the first copy, the other TFM copies get the fixed text (`WithDocumentText`). So findings appear once, and all copies stay equal so that `ProjectPackages` decides correctly.
- `ProjectPackages.PlanAsync` deduplicates by project path; `KeepRemovedPackage` if one TFM still uses the package.
- `FixCommand` counts/diffs changed documents uniquely per path.
- Tests: `FixerTests.FixAsync_SameFileInTwoTargetFrameworks_ReportsOnceAndFixesBothCopies`, `SampleShopTests.ScanFix_MultiTargetProject_ReportsAndWritesEachFileOnce` (net8.0;net10.0, project inline in a temp folder).
- Shortcut: `#if` code that differs per TFM is only examined with the first TFM.

## See also

- `claude/tasks/TAAK-019-lancering.md`
- `claude/tasks/TAAK-021-fa-nullable-subject.md`
