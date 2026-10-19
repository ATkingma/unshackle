# TAAK-292 — Council follow-up: scoped IMapper lookup removal, Info lines, 0.2.0 checks

**Status**: step 1/4 — not started
**Created**: 2026-10-19
**Goal**: implement the council decisions of 2026-10-19 (`claude/docs/council.md`) with proof.

## Context for the next agent

- Decision 3: replace the solution-wide `bool registered` in `MapperCleanup` (around lines 239-249) by the scoped three-condition rule in `council.md`; the kept-lookup report says which condition failed (file:line).
- Decision 4: when unused-imapper leaves an empty constructor or empty factory class, report an Info line; do not remove it.
- Decisions 1 and 2 need no code: check the starter and fullcart reasons name the blocker (fullcart: the Scrutor decorator line).

## Steps

- [ ] 1. KingmaDoc plan update (unused-imapper REQ for the scoped rule and Info line)
- [ ] 2. Implementation + unit tests + proofs: own project / dependent host / conditional call / second container without AddAutoMapper; overload/partial-type test for the plan cache; end-to-end test that one remaining `IMapper` use keeps `AddAutoMapper`; explicit pattern order check
- [ ] 3. Corpus `after` on pi22317, assistiva, culinary, msb, encryptednotes, estore: 0 regressions, numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

Not in scope (separate task before 0.2.0): full blind corpus rerun on current main; public-constructor argument removal for external consumers (put to the council if a choice is needed).

## See also

- `claude/docs/council.md`, `claude/tasks/archief/TAAK-289-automapper-imapper-still-injected.md`
