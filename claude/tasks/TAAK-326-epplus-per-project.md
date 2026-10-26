# TAAK-326 — EPPlus per-project migration (council round 13, E17)

**Status**: step 3/5 — in progress
**Created**: 2026-10-26
**Goal**: the EPPlus fix migrates per project instead of all-or-nothing per solution (0% Fixed over 38 corpus repos was the trigger).

## Context for the next agent

- Rule (E17): a project migrates when it has 0 blocking rows and no EPPlus type crosses to a project that stays (else Manual
  `EPPlusTypeCrossesProject`). No per-file mode. EPPlus is removed per project only when nothing there uses it. No opt-out flag.
- Built as `IPartialFix` on `EPPlusFix` (reuses TAAK-274 `PartialRun`): `src/Unshackle.Core/Fixes/EPPlus/EPPlusProjects.cs`.
  Kept projects: every document root annotated; one `epplus.kept` row per project (`KeptReason.EPPlusProjectKept`).
- Core changes: `KeptReason.EPPlusProjectKept` + `KeptReasonText.EPPlusPrefix`; `ProjectFileMigrations.Blocker` treats that row as
  blocking (no E7 pin on a kept project); `PartialRun` treats a re-reported root (G4 row) as planned.
- 2026-10-26: picked up by Claude Opus 5.5 (main session).

## Steps

- [x] 1. Planner + Core seam (`EPPlusProjects`, `IPartialFix`)
- [x] 2. Unit tests `src/Unshackle.Tests/Core/EPPlus/EPPlusPerProjectTests.cs` (clean + blocked, cross-project type, all blocked); docs (`core.md`, `epplus-package-plan.md`, `epplus-unsupported-plan.md`)
- [→] 3. Two-project sample in `samples/` with an integration test
- [ ] 4. Corpus re-measure with Fixed % per repo (`CORPUSRUN-epplus.md`)
- [ ] 5. Update TAAK-309 status, archive

## Changed files

- `src/Unshackle.Core/Fixes/EPPlus/EPPlusProjects.cs` (new), `EPPlusFix.cs`, `EPPlusReason.cs`
- `src/Unshackle.Core/Models/KeptReason.cs`, `KeptReasonText.cs`, `src/Unshackle.Core/ProjectFileMigrations.cs`, `src/Unshackle.Core/PartialRun.cs`
- `src/Unshackle.Tests/Core/EPPlus/EPPlusPerProjectTests.cs` (new)

## See also

- `claude/tasks/COUNCIL-round-13.md` (E17), `claude/tasks/TAAK-309-epplus-to-closedxml.md`, `claude/tasks/CORPUSRUN-epplus.md`
