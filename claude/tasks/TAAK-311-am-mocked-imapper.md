# TAAK-311 — AutoMapper: classes under test with a mocked IMapper

**Status**: step 1/3 — not started
**Created**: 2026-10-22
**Goal**: Migrate call sites of classes whose tests hand them a mocked IMapper, instead of keeping IMapper, its profile maps and AddAutoMapper.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 1). Repos: blind culinary, encryptednotes, estore, msb (~100 rows incl. kept-map and add-automapper follow-ups).
- Design choice for the council: rewrite the mock set-ups (`Setup(m => m.Map<X>(..))`) to use the generated method, or keep the mock and only migrate production code.
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); KingmaDoc plan + verify per changed pattern.

## Steps

- [ ] 1. Council decision on the approach; then plan + verify
- [ ] 2. Implement in the pattern with proofs in `src/Unshackle.Tests/Proofs/`
- [ ] 3. Corpus check on the named repos (main session runs it), 0 regressions; update docs

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | Core/Fixes/AutoMapper (map-call, kept-map, test set-ups) |
| **How**   | Pattern change + proofs |
| **Why**   | Unblocks repos that now stay (partly) Manual |

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — ranking this task comes from
- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — parent task
- `claude/docs/INDEX.md` — documentation index
