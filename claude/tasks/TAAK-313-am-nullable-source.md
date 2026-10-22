# TAAK-313 — AutoMapper: nullable source into a non-nullable target

**Status**: step 1/3 — not started
**Created**: 2026-10-22
**Goal**: Migrate Map calls whose source may be null by emitting a null check with AutoMapper's exact semantics.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 3). Repos: stale automapper cats, kavita, lancommander, letterbook, personalasst (+1).
- AutoMapper returns null for a null source; the generated extension throws. Only rewrite where the result keeps the same nullability behaviour.
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); KingmaDoc plan + verify per changed pattern.

## Steps

- [ ] 1. Re-measure the automapper corpus (data predates TAAK-271..298); plan + verify
- [ ] 2. Implement in the pattern with proofs in `src/Unshackle.Tests/Proofs/`
- [ ] 3. Corpus check on the named repos (main session runs it), 0 regressions; update docs

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | Core/Fixes/AutoMapper (map-call) |
| **How**   | Pattern change + proofs |
| **Why**   | Unblocks repos that now stay (partly) Manual |

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — ranking this task comes from
- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — parent task
- `claude/docs/INDEX.md` — documentation index
