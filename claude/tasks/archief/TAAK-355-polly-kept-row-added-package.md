# TAAK-355 — Polly kept row names the added package

**Status**: done
**Created**: 2026-10-27
**Goal**: Council round 22 P11 (`claude/tasks/COUNCIL-round-22.md`): the kept "Polly is still used ... stay" row of a project with manual
findings says "; Microsoft.Extensions.Resilience was added next to it" when, and only when, the package migration added it in this run.

## Context for the next agent

- Origin: `claude/tasks/archief/TAAK-354-polly-downgrade-transitive-cpm.md` (babylon-gateway `RadixDlt.CoreApiSdk.csproj` rows looked contradictory).
- Use the generic "added next to it" text in `src/Unshackle.Core/Fixer.cs` (`KeptPackageReason`, council 14 M10); do not hardcode it in
  `src/Unshackle.Core/Fixes/Polly/`. First find why the Polly kept row does not get it today (`Fixer.MigrateProjectFilesAsync`).

## Steps

- [x] 1. Wire the Polly kept row to the shared "added next to it" text; clause only when the add was applied
- [x] 2. Unit tests: added case (clause present) and blocked case, e.g. `PackageDowngrade` (no clause); update expectations
- [x] 3. `dotnet build`, `dotnet test`, verify-head; archive

## Result

- `Fixer.KeptPackageReason` no longer ends in ". "; the plain kept row is now `{name}{KeptPackageReason}{AddedNextTo(m, changes)}. Fix the manual
  findings ...`, the same council 14 M10 clause the kept-for-dependent row already had. It applies to every fix, not only Polly.
- Tests: `PollyFixTests.FixAsync_KeptPollyProject_RowNamesResilienceOnlyWhenTheEditAddedIt` (added / not added) and
  `FixAsync_PackageDowngradeBlocked_NoRowClaimsTheAdd`.

## See also

- `claude/tasks/COUNCIL-round-22.md` — P11
- `claude/docs/council.md` — decision council
