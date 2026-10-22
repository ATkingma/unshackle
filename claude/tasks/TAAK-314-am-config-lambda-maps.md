# TAAK-314 — AutoMapper: maps in a MapperConfiguration lambda

**Status**: step 1/3 — not started
**Created**: 2026-10-22
**Goal**: Treat CreateMap calls inside `new MapperConfiguration(cfg => ...)` like a profile constructor body.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 4). Repos: stale automapper gnomeshade, letterbook, personalasst, xipona (89 rows).
- Today only Profile constructors are rewritten.
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); KingmaDoc plan + verify per changed pattern.

## Steps

- [ ] 1. Re-measure; plan + verify
- [ ] 2. Implement in the pattern with proofs in `src/Unshackle.Tests/Proofs/`
- [ ] 3. Corpus check on the named repos (main session runs it), 0 regressions; update docs

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | Core/Fixes/AutoMapper (create-map, map-call) |
| **How**   | Pattern change + proofs |
| **Why**   | Unblocks repos that now stay (partly) Manual |

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — ranking this task comes from
- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — parent task
- `claude/docs/INDEX.md` — documentation index
