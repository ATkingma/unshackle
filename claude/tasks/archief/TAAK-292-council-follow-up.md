# TAAK-292 — Council follow-up: scoped IMapper lookup removal, Info lines, 0.2.0 checks

**Status**: ✅ done
**Created**: 2026-10-19
**Goal**: implement the council decisions of 2026-10-19 (`claude/docs/council.md`) with proof.

## Context for the next agent

- Decision 3: replace the solution-wide `bool registered` in `MapperCleanup` (around lines 239-249) by the scoped three-condition rule in `council.md`; the kept-lookup report says which condition failed (file:line).
- Decision 4: when unused-imapper leaves an empty constructor or empty factory class, report an Info line; do not remove it.
- Decisions 1 and 2 need no code: check the starter and fullcart reasons name the blocker (fullcart: the Scrutor decorator line).

## Steps

- [x] 1. KingmaDoc plan update (unused-imapper REQ for the scoped rule and Info line)
- [x] 2. Implementation + unit tests + proofs: own project / dependent host / conditional call / second container without AddAutoMapper; overload/partial-type test for the plan cache; end-to-end test that one remaining `IMapper` use keeps `AddAutoMapper`; explicit pattern order check
- [x] 3. Corpus `after` on pi22317, assistiva, culinary, msb, encryptednotes, estore: 0 regressions, numbers here and in CORPUSRUN-blind.md
- [x] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

Not in scope (separate task before 0.2.0): full blind corpus rerun on current main; public-constructor argument removal for external consumers (put to the council if a choice is needed).

## Result

- `MapperRegistration` (new) implements the three-rule scope; `MapperCleanup` plans twice to find lookups kept only by the rule and reports them as `Stays on AutoMapper:` Manual with the rule and file:line. `FixedResult.Info` (new, optional) carries the "now empty" line; `ReportRow` shows it after the warning.
- Corpus: 0 regressions, counts unchanged (`CORPUSRUN-blind.md`, section TAAK-292).
- Open: models figure 1 and 4 not rendered (no `kingmadoc`/`d2` here); fullcart reason says "Autofac" for a Scrutor `Decorate`.

## See also

- `claude/docs/council.md`, `claude/tasks/archief/TAAK-289-automapper-imapper-still-injected.md`
