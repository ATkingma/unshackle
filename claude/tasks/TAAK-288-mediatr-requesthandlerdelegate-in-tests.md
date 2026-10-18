# TAAK-288 — MediatR: `RequestHandlerDelegate` with a real delegate in tests

**Status**: step 1/4 — not started
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- afripay (0/16) and cleanmicro (0/20) stop because tests create `RequestHandlerDelegate<T>` from a real lambda (not a mock) to call a pipeline behavior directly. fullcart keeps 15 handlers on MediatR because of an Autofac decorator: confirm whether that is principally manual.
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Probe how Mediator's behavior signature (`MessageHandlerDelegate<TMessage,TResponse>`, `ValueTask`) can express the same test call; rewrite the test-side delegate when equivalent, else Manual with the reason. Decide fullcart's decorator case and record it.

## Steps

- [ ] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on afripay, cleanmicro, fullcart: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
