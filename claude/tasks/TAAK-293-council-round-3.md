# TAAK-293 — Council round 3: typed kept-lookup reason, stricter registration rules, fullcart text

**Status**: step 1/4 — not started
**Created**: 2026-10-19
**Goal**: implement the round-3 council decisions in `claude/docs/council.md` with proof.

## Context for the next agent

- Builds on TAAK-292 (`claude/tasks/archief/TAAK-292-council-follow-up.md`): `MapperRegistration.cs`, `MapperCleanup.cs`, `FixedResult.Info`.
- Q1: make sure `Info` also appears in the JSON/markdown report output.
- Q2: replace the `Stays on AutoMapper:` string prefix as mechanism by a typed reason code on the finding; the renderer prints the prefix; a test pins the rendered text.
- Q3: target-typed `new()` recognised as container via converted type; `AddAutoMapper` inside a lambda only unconditional when the lambda goes directly to an unconditional registration/builder call, else keep with reason `ConditionalOrDeferredRegistration` + file:line.
- fullcart: the MediatR reason names Scrutor's `Decorate(typeof(INotificationHandler<>), …)` instead of Autofac.

## Steps

- [ ] 1. KingmaDoc plan updates (unused-imapper; the MediatR pattern with the decorator reason)
- [ ] 2. Implementation + unit tests + proofs
- [ ] 3. Corpus `after` on pi22317, assistiva, culinary, msb, encryptednotes, estore (+ `scan` on fullcart for the text): 0 regressions, numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/docs/council.md`, `claude/tasks/archief/TAAK-292-council-follow-up.md`
