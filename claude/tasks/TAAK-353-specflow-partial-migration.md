# TAAK-353 — SpecFlow → Reqnroll: per-project partial migration (backlog)

**Status**: steps 1-3 done (branch `taak-340g`); step 4 (corpus) with TAAK-340 step 7
**Created**: 2026-10-27
**Goal**: Let the SpecFlow fix migrate the projects without blocking rows while the blocked ones stay on SpecFlow (council round 18, S2).

## Context for the next agent

- Today `SpecFlowFix.AllOrNothing` is true: any Manual row (string step call, `async void`, packages.config, an unported plugin, a
  `Directory.Build.props` blocker) leaves the whole solution on SpecFlow. The run summary names every blocking project and its rows (S2, TAAK-340 step 3a).
- Council 18 S2: the unit of a partial migration is a project **plus the binding projects it references** (step definitions live in
  referenced class libraries; a test project on Reqnroll with a binding library on SpecFlow does not run).
- Existing mechanism: `IPartialFix` / `PartialRun` (TAAK-274, MediatR). It keeps code tied to Manual rows on the old library; SpecFlow needs
  whole units instead, so a new plan type or a unit-based variant is likely.

## Steps

- [x] 1. Design: compute units (project + referenced projects that hold `[Binding]` classes); a unit with a blocking row stays whole; a
  binding project shared by a blocked and a free unit blocks both
- [x] 2. Shared `Directory.*.props` declarations: a props file imported by a blocked unit stays (G3 already blocks the edit)
- [x] 3. Implement (`SpecFlowProjects`, `SpecFlowFix : IPartialFix`, `KeptReason.SpecFlowProjectKept`), two- and three-project tests in
  `SpecFlowPerProjectTests` (no proof: the proof harness is single-project), KingmaDoc update in `specflow-namespace-plan.md`, `core.md`
- [ ] 4. Corpus check against the TAAK-340 corpus round

## Design (as built)

- Kept = the owners of every blocking row (a project-file row by its path, a document row by every project compiling it). Closure until
  stable: projects sharing a file with a kept one; for every SpecFlow project and each SpecFlow project it references (transitively,
  also through non-SpecFlow projects), either side kept keeps the other. This is stricter than S2's "a shared binding project blocks
  both": any link keeps both, which also covers a binding library that references another binding library.
- A row outside every project (a shared props file, G3 `SharedDeclarationBlocked`) returns no plan: all-or-nothing, as for EPPlus.
- Kept projects get one `specflow.kept` row, and no edits (G4); their Info rows are not suppressed (the specflow.json / feature.cs Info still
  shows for a kept project; harmless).

## Council questions

1. A non-SpecFlow project that references both a kept and a migrated unit (a shared helper) is not touched; both packages then reach
   any project referencing both units. The closure keeps such units apart only through SpecFlow links. Acceptable, or keep both units
   when a third project references both?

## See also

- `claude/tasks/TAAK-340-specflow-to-reqnroll.md` — the SpecFlow fix (step 3f)
- `claude/tasks/COUNCIL-round-18.md` — S2
