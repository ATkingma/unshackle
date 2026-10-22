# TAAK-312 — MediatR: resolve AddMediatR scans of non-literal assemblies

**Status**: step 1/3 — not started
**Created**: 2026-10-22
**Goal**: Resolve the scanned assembly when AddMediatR gets it from a type parameter or a method parameter whose callers are all in the solution.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 2). Repos: blind miru, starter (whole solutions blocked); stale mediatr evo1, evo2.
- Non-public handlers (ardalis, evo2) stay justified unless the generated code can see them; MediatR stays all-or-nothing, so one root here blocks a whole repo.
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); KingmaDoc plan + verify per changed pattern.

## Steps

- [ ] 1. Re-measure the mediatr corpus; list the call shapes; plan + verify
- [ ] 2. Implement in the pattern with proofs in `src/Unshackle.Tests/Proofs/`
- [ ] 3. Corpus check on the named repos (main session runs it), 0 regressions; update docs

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | Core/Fixes/MediatR (add-mediatr) |
| **How**   | Pattern change + proofs |
| **Why**   | Unblocks repos that now stay (partly) Manual |

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — ranking this task comes from
- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — parent task
- `claude/docs/INDEX.md` — documentation index
