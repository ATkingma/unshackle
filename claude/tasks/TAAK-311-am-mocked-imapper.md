# TAAK-311 — AutoMapper: classes under test with a mocked IMapper

**Status**: step 3/3 — implemented (branch worktree-agent-a2af3d34de85010f0); corpus round by the main session pending
**Created**: 2026-10-22
**Goal**: Migrate call sites of classes whose tests hand them a mocked IMapper, instead of keeping IMapper, its profile maps and AddAutoMapper.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 1). Repos: blind culinary, encryptednotes, estore, msb (~100 rows incl. kept-map and add-automapper follow-ups).
- Design choice for the council: rewrite the mock set-ups (`Setup(m => m.Map<X>(..))`) to use the generated method, or keep the mock and only migrate production code.
- Rule: only reduce Manual with proof (proofs + 0 corpus regressions); KingmaDoc plan + verify per changed pattern.

## Steps

- [x] 1. Council decision on the approach (round 8, A1); plan + verify `docs/projectinfo/patterns/automapper-mock-set-up-*`
- [x] 2. Implemented: new pattern `automapper.mock-set-up` (`MockSetUpPattern`, `MockedMappers`), plan changes in `MapOwners`
  (candidates, veto-and-rebuild), `MapCallPattern.FindAsync`; proof `_Combined/mocked-stub-removed`; 13 unit tests in
  `src/Unshackle.Tests/Core/AutoMapper/MockSetUpPatternTests.cs`
- [x] 3. Scan of the four blind repos (read-only copies): Manual culinary 70 → 70, encryptednotes 6 → 6, estore 22 → 22, msb 29 → 29,
  no status change; reasons now name the test file:line and why. Full corpus round: main session.

## Result (2026-10-22)

- Rule A1 as built: a stub goes only when the mock's every use is its creation, a constructor argument or a stub; every stub's source and
  value are `new` objects of the test member that never change, the map is a plain `CreateMap<A, B>()` (profile without settings), each
  settable member of B is a same-named simple member of A and the value equals the source's member (constant or read from it); the test
  uses the value only member by member (or B is a record); and, once the plan is stable, every Map call of the classes is migrated and
  stubbed by that mock. Each removed stub warns. Otherwise the stub stays and the Manual row names the test and the reason.
- Corpus blockers are all real (would break or weaken the test): argument matchers, `Verify` of Map, the stubbed object reused by
  reference, mocks without stubs, a map with options. A further reduction needs a council call (see questions below).
- Open for the main session: add the `automapper.mock-set-up` row to `docs/patterns.md` (generated) and `docs/projectinfo/README.md`.

## Council questions

1. `Verify(m => m.Map<B>(x), Times.Once)`: may the rewrite drop it (with a warning), as M4 does for Moq checks? (culinary 7, msb 2)
2. A stub with an argument matcher (`It.IsAny<A>()`) whose test only reads members that the generated map copies from a source the
   test controls: allowed, or always Manual? (culinary 17, msb 9)
3. A mock without stubs (loose, answers null): allowed when the test asserts nothing about the mapped result? (culinary 3, encryptednotes 1)

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
