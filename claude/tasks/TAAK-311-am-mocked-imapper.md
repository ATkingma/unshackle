# TAAK-311 — AutoMapper: classes under test with a mocked IMapper

**Status**: step 4/4 — A1 implemented (branch worktree-agent-a2af3d34de85010f0); A6–A8 implemented (branch worktree-agent-a04c16b800e2dab3f); corpus round by the main session pending
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
- [x] 4. Council round 11 follow-ups A6–A8 (agent am-mt-followups): typed codes `MockSetUpCode` (`VerifyOnMapper`,
  `MockedReturnNotProvable`, `MockWithoutStubsNotProvable`) through `ManualResult.Code`; A6 checks of a Map call Manual; A7 matcher stubs;
  A8 mocks without stubs with an Info row. Proofs `MapCall/mocked-verify`, `_Combined/mocked-matcher-stub-removed`,
  `_Combined/mocked-no-stubs`; 15 new test cases in `MockSetUpPatternTests` (28 in total). Blind repos: Manual unchanged (70/6/22/29).

## Result (2026-10-22)

- Rule A1 as built: a stub goes only when the mock's every use is its creation, a constructor argument or a stub; every stub's source and
  value are `new` objects of the test member that never change, the map is a plain `CreateMap<A, B>()` (profile without settings), each
  settable member of B is a same-named simple member of A and the value equals the source's member (constant or read from it); the test
  uses the value only member by member (or B is a record); and, once the plan is stable, every Map call of the classes is migrated and
  stubbed by that mock. Each removed stub warns. Otherwise the stub stays and the Manual row names the test and the reason.
- Corpus blockers are all real (would break or weaken the test): argument matchers, `Verify` of Map, the stubbed object reused by
  reference, mocks without stubs, a map with options. A further reduction needs a council call (see questions below).
- Open for the main session: add the `automapper.mock-set-up` row to `docs/patterns.md` (generated) and `docs/projectinfo/README.md`.

## Result round 12 (2026-10-22, A9, A10)

- A9: A8 stays strict (any read of the mapped value blocks).
- A10: the one A of a matcher stub may be created in place (literal or no constructor arguments); a member it leaves
  default-constructed that the test reads, or one left to constructor arguments, keeps the stub Manual `MockedReturnNotProvable`.
  Tests: 3 new cases (31 in MockSetUpPatternTests).

## Result round 11 (2026-10-22, A6–A8)

- A6: `Verify(m => m.Map..., Times.X)` (any `Times`) and NSubstitute `Received`/`DidNotReceive`(`WithAnyArgs`) of a Map call keep the mock;
  the checked call gets its own Manual row `VerifyOnMapper` ("assert the mapped result instead"), the mock's stubs carry the same code.
- A7: a stub with `It.IsAny<A>()` / `Arg.Any<A>()` uses the one `A` the test member builds (exactly one plain `new A { ... }` local, no
  other expression of type `A` outside the stub); then every A1 rule applies unchanged (all settable members, not only asserted ones;
  the value is not used whole unless B is a record). Otherwise Manual `MockedReturnNotProvable` (also given to A1 stubs that fail).
- A8: a mock without stubs is a candidate; it is discharged only when every Map call of its classes is a plain `Map` whose value nobody
  reads (statement, discard, unread local). Info row on the mock; otherwise Manual `MockWithoutStubsNotProvable` naming the call.
- Blind repos, Manual before → after: culinary 70 → 70, encryptednotes 6 → 6, estore 22 → 22, msb 29 → 29. All matcher stubs fail the
  "one A in a local" check (the source is built in place in a repository stub or not at all) and most compare the stub whole; the
  stub-less mocks pass the value on. These rows now carry the codes (VerifyOnMapper 17, MockedReturnNotProvable 26).
- Open for the main session: `docs/patterns.md` row text unchanged; `CombinedProofTests` now accepts Info rows in AutoMapper combined proofs.

## Council questions (round 11 follow-ups)

4. A8 as built is stricter than the decision: a member read or null check of the value inside the method also blocks, because on the
   loose mock that read throws (or the check branches) and a try/catch in the method can turn it into a different result. Keep strict?
5. A7 culinary/msb shape: the source is created in place inside a repository stub (`ReturnsAsync(Result<Account>.Success(new Account()))`).
   Should "built in the test" cover an in-place creation that is the only `A` in the member? (It would not unblock these rows by itself:
   most also compare the stubbed object whole or set values not read from the source.)

## Council questions (round 8, answered in round 11)

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
