# TAAK-356 — EF bulk: kept-for-dependent row on a project without a Z package

**Status**: done (2026-10-28)
**Created**: 2026-10-28
**Goal**: no "stays in this project because a project that references it still uses it" row for a project that references neither
`Z.EntityFramework.Plus.EFCore` nor `Z.EntityFramework.Extensions.EFCore`.

## Context for the next agent

- Found in the EF bulk corpus round (`../CORPUSRUN-efbulk.md`, repo morenote). `MoreNote.Config`, `MoreNote.CryptographyProvider`
  and `MoreNote.SignatureService` have no Z reference, yet each gets the row with the B13 fallback name
  "Z.EntityFramework.Plus.EFCore or Z.EntityFramework.Extensions.EFCore" (`EfBulkFix.KeptPackageName`: `named.Count == 0` → fallback).
  The fallback is meant only for an unreadable project file (council round 21 B13); here the file was readable.
- Rows are built in `src/Unshackle.Core/Fixer.cs` (`KeptForDependentReason`, `migrations.Where(m => m.KeepRemovedPackage)`). Find first why a
  project without the package is a kept migration at all (generic engine or EF bulk only?); fix the root cause there, not only the name.
- Minimal proof (not run, `_Pending`): `src/Unshackle.Tests/Proofs/EfBulk/_Pending/KeptRow/referenced-project-without-z.*`. A project-level
  case needs a unit test with two projects in `src/Unshackle.Tests/Core/EfBulk/EfBulkFixTests.cs` (see the B13 Extensions-only test).

## Steps

- [x] 1. Unit test (two projects, dependent without Z) that fails today
- [x] 2. Root-cause fix; move the proof out of `_Pending` or drop it in favour of the unit test
- [ ] 3. Rerun morenote (`claude/tools/corpus-efbulk-run.sh after morenote`), update `CORPUSRUN-efbulk.md`

## Result

- Root cause in the generic engine (`ProjectPackages.KeepForDependents`, shared by every fix with a package migration), not EF bulk only:
  every project in a keeping project's reference closure was kept, also one that gets the package only through a project it references
  itself (morenote: Logic -> Config -> Models, only Models declares Z). Now such a project is not kept; the providing project further down
  is in the same closure and keeps the package, so the dependent still compiles.
- Test: `EfBulkFixTests.FixAsync_ReferencedProjectGetsZOnlyTransitively_NoKeptRow` (red before, green after). The `_Pending` proof was
  dropped: a project-level case is not a single-file proof.
- Step 3 (morenote rerun) not done in this round; the unit test covers the morenote shape.

## See also

- `../TAAK-342-ef-bulk.md` — EF bulk fix (step 9 corpus round)
- `../CORPUSRUN-efbulk.md` — corpus results
