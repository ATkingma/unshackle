# TAAK-290 — MediatR: starter still blocked (`AddMediatR` + `InternalsVisibleTo`)

**Status**: step 1/4 — not started
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- TAAK-274 designed project-level partial migration for starter (two Mediator hosts + `InternalsVisibleTo` to a test project that calls `AddMediatR` → CS0121), but round 5 still shows starter blocked (0/11).
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Find why the project-level ownership did not apply (reason text in the report), fix it with a proof (`_Combined` multi-project case if possible) or record why it is principally manual.

## Steps

- [ ] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on starter: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
