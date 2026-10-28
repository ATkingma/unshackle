# TAAK-356 — EF bulk: kept-for-dependent row on a project without a Z package

**Status**: open
**Created**: 2026-10-28
**Goal**: no "stays in this project because a project that references it still uses it" row for a project that references neither
`Z.EntityFramework.Plus.EFCore` nor `Z.EntityFramework.Extensions.EFCore`.

## Context for the next agent

- Found in the EF bulk corpus round (`claude/tasks/CORPUSRUN-efbulk.md`, repo morenote). `MoreNote.Config`, `MoreNote.CryptographyProvider`
  and `MoreNote.SignatureService` have no Z reference, yet each gets the row with the B13 fallback name
  "Z.EntityFramework.Plus.EFCore or Z.EntityFramework.Extensions.EFCore" (`EfBulkFix.KeptPackageName`: `named.Count == 0` → fallback).
  The fallback is meant only for an unreadable project file (council round 21 B13); here the file was readable.
- Rows are built in `src/Unshackle.Core/Fixer.cs` (`KeptForDependentReason`, `migrations.Where(m => m.KeepRemovedPackage)`). Find first why a
  project without the package is a kept migration at all (generic engine or EF bulk only?); fix the root cause there, not only the name.
- Minimal proof (not run, `_Pending`): `src/Unshackle.Tests/Proofs/_Pending/EfBulk/KeptRow/referenced-project-without-z.*`. A project-level
  case needs a unit test with two projects in `src/Unshackle.Tests/Core/EfBulk/EfBulkFixTests.cs` (see the B13 Extensions-only test).

## Steps

- [ ] 1. Unit test (two projects, dependent without Z) that fails today
- [ ] 2. Root-cause fix; move the proof out of `_Pending` or drop it in favour of the unit test
- [ ] 3. Rerun morenote (`claude/tools/corpus-efbulk-run.sh after morenote`), update `CORPUSRUN-efbulk.md`

## See also

- `claude/tasks/TAAK-342-ef-bulk.md` — EF bulk fix (step 9 corpus round)
- `claude/tasks/CORPUSRUN-efbulk.md` — corpus results
