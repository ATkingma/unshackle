# TAAK-322 — Engine follow-ups from council round 12 (G6–G9)

**Status**: step 1/4 — not started
**Created**: 2026-10-22
**Goal**: Solution-level CPM decision, comment kept before a removed first using, and a pre-filter that matches old-library assemblies, with existing output byte-identical except where a rule changes it on purpose.

## Context for the next agent

- Spec: `claude/docs/council.md`, round 12 (G6, G7, G8, G9). G6 is mostly done by TAAK-321; check and add a test.
- G9 cause: PollyFix rewrote nothing in real runs (empty `RemovedNamespaces`, pre-filter skipped every file); fixed with `FindsOnlyOldLibraryCode => false`, pinned by `Core/EndToEnd/NewFixesSmokeTests`.

## Steps

- [ ] 1. G9: optional fix-level old-library assemblies/package ids for the pre-filter; Polly declares them and drops the escape hatch; contract test: every fix gives identical output with the pre-filter on and off on its proofs
- [ ] 2. G7: solution-level pass per `Directory.Packages.props`
- [ ] 3. G8: leading trivia of a removed first using moves to the next node (with C3)
- [ ] 4. G6 test; `core.md`; build 0 warnings, all tests green

## Progress log

- 2026-10-22: created after council round 12.

## See also

- `claude/docs/council.md` — round 12
- `claude/tasks/TAAK-321-blockers-stop-package-swap.md`
