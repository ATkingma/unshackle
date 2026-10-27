# TAAK-353 — SpecFlow → Reqnroll: per-project partial migration (backlog)

**Status**: backlog
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

- [ ] 1. Design: compute units (project + referenced projects that hold `[Binding]` classes); a unit with a blocking row stays whole; a
  binding project shared by a blocked and a free unit blocks both
- [ ] 2. Shared `Directory.*.props` declarations: a props file imported by a blocked unit stays (G3 already blocks the edit)
- [ ] 3. Implement, proofs on a two-unit sample, KingmaDoc update of the SpecFlow fix docs
- [ ] 4. Corpus check against the TAAK-340 corpus round

## See also

- `claude/tasks/TAAK-340-specflow-to-reqnroll.md` — the SpecFlow fix (step 3f)
- `claude/tasks/COUNCIL-round-18.md` — S2
