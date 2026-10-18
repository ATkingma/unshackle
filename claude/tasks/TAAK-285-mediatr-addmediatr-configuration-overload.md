# TAAK-285 — MediatR: `AddMediatR(Action<MediatRServiceConfiguration>)` overload

**Status**: step 1/4 — not started
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- projectpulse (round 5, 0 Fixed / 176 Manual) stops because the `AddMediatR(Action<MediatRServiceConfiguration>)` overload is not matched; one registration blocks the whole solution.
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Measure the overload forms in MediatR 12.5.0 (`RegisterServicesFromAssembly*`, `AddOpenBehavior`, `AddBehavior`, `Lifetime`, `NotificationPublisher`…) and map each to `AddMediator(options => …)` only where Mediator behaves the same; unknown options stay Manual with the option named in the reason.

## Steps

- [ ] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on projectpulse (and the earlier MediatR corpus repos): build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
