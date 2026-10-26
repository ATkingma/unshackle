# TAAK-315 — MediatR: generic messages and handlers

**Status**: done 2026-10-26 — no change needed (re-measure: kgr 90 → 0, eshop/jason 0; already solved by TAAK-193 generic-request/notification; nadir source gone, not measured). Archive.
**Created**: 2026-10-22
**Goal**: Decide (council) whether generic requests/handlers are closed per used type argument, or stay justified Manual.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 5). Repos: stale mediatr eshop, kgr, nadir (34 rows).
- Mediator's source generator does not support generic messages; closing them changes user types, so this may stay justified.
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); KingmaDoc plan + verify per changed pattern.

## Steps

- [ ] 1. Council question; if go, plan + verify
- [ ] 2. Implement in the pattern with proofs in `src/Unshackle.Tests/Proofs/`
- [ ] 3. Corpus check on the named repos (main session runs it), 0 regressions; update docs

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | Core/Fixes/MediatR (unsupported) |
| **How**   | Pattern change + proofs |
| **Why**   | Unblocks repos that now stay (partly) Manual |

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — ranking this task comes from
- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — parent task
- `claude/docs/INDEX.md` — documentation index
