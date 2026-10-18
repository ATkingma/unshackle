# TAAK-289 — AutoMapper: `AddAutoMapper` stays because `IMapper` is still injected

**Status**: step 1/4 — not started
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- pi22317 and assistiva migrate most maps but keep `AddAutoMapper` because some class still injects `IMapper` without using it for a kept map (or uses it only for migrated pairs).
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Find every remaining `IMapper` injection; when all its calls are migrated, remove the field/parameter/argument (TAAK-272 clean-up) also across DI registrations and test constructors; keep it only when a kept map needs it. Then drop `AddAutoMapper` and the package when no map remains.

## Steps

- [ ] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on pi22317, assistiva, culinary, msb, encryptednotes, estore: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
