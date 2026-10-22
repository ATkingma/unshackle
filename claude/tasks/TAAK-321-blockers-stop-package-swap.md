# TAAK-321 — Project blockers also stop the package swap

**Status**: step 2/2 — done (engine-2)
**Created**: 2026-10-22
**Goal**: A project with a `ProjectBlockingReason` row keeps its old package and gets no new package from `fix.Package` (`MigrateProjectFilesAsync`), so no target package is added next to a kept old one.

## Context for the next agent

- Found by the Moq agent (Q3): in `Fixer.FixAsync` the package migration runs before the `ProjectFileMigrations` step, so a project blocked by `TargetFrameworkUnsupported` can still get NSubstitute added while Moq stays. TAAK-320 already keeps the documents unrewritten; this closes the package side.
- Follows council round 8 (package removal per project only at 0 Manual) and round 10 G4. Output of fixes without blockers must stay byte-identical.

## Steps

- [x] 1. Package migration skips projects with blocking rows (both remove and add); the report row says "package kept: blocked by <code>"
- [x] 2. Tests (fake fix with a blocker), `core.md`; build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after Moq Q3.
- 2026-10-22: done. Blocked projects are filtered out of the package migrations, with a Manual "package kept: blocked by <code>" row; 2202 tests green.

## See also

- `claude/tasks/TAAK-320-blockers-stop-rewrites.md`
- `claude/docs/council.md` — rounds 8 and 10
