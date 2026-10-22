# Verification: automapper-mock-set-up

|               |                                                                                          |
| ------------- | ---------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-mock-set-up-plan.md`](automapper-mock-set-up-plan.md) |
| **Status**    | Verified (pending the generated row in `docs/patterns.md`, see Deviations)               |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                       |

> Verified by an AI agent against the code of branch `worktree-agent-a2af3d34de85010f0` (TAAK-311).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `_Combined/mocked-stub-removed`; `FixAsync_StubEqualToGeneratedMap_RemovesStubAndMigratesClassUnderTest` (NSubstitute), `FixAsync_MoqStubEqualToGeneratedMap_RemovesStub` (Moq shape, through an in-memory stand-in assembly named `Moq`) |
| REQ-2 | Met     | `FixAsync_StubEqualToGeneratedMap_…` asserts the warning `The stub of IMapper.Map<CustomerDto> at Test.cs:13 is removed` |
| REQ-3 | Met     | theory `FixAsync_StubNotProvablyEqual_KeepsStubAndNamesTest` rows: other constant, `Arg.Any`, object changed after creation |
| REQ-4 | Met     | same theory, row `Received()` (`the mock is used another way at Test.cs:`) |
| REQ-5 | Met     | theory row `ReferenceEquals` (kept); `FixAsync_RecordComparedWhole_RemovesStub` |
| REQ-6 | Met     | `FixAsync_MapWithOptions_KeepsStub`, `FixAsync_ProfileSetting_KeepsStub`, `FixAsync_OptionsOnAnotherMap_RemovesStub` |
| REQ-7 | Met     | `FixAsync_ClassMapsAPairTheMockDoesNotStub_KeepsStub` (veto and rebuild: `the map Order → OrderDto at Service.cs:… is not stubbed by this mock`) |
| REQ-8 | Met     | `FixAsync_MockWithoutStub_NamesTheTestOfTheClassUnderTest` (`(test: Test.cs:10: the mock stubs no map …)`) |
| REQ-9 | Met     | all earlier AutoMapper proofs and `MapOwnersTests` unchanged and green; blind corpus scan below: same Fixed/Manual per row |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 1687 total, 1686 passed, 1 failed: `PatternDocTests.PatternsDoc_MatchesListMarkdown`
  needs the new `automapper.mock-set-up` row in `docs/patterns.md` (a shared file outside this task's claim; the main session adds it).
- Blind corpus, `scan` with the branch tool (read-only copies), AutoMapper rows Manual before (after-298) → after: culinary 70 → 70,
  encryptednotes 6 → 6, estore 22 → 22, msb 29 → 29. No row changed status; the Manual reasons now name the test and why. Blockers found:
  argument matchers (culinary 17, msb 9), `Verify` of the Map call (culinary 7, msb 2), the stubbed object reused by reference in another
  stub or assertion (estore 6, msb 3), a mock without stubs (culinary 3, encryptednotes 1), a map with options (culinary `Review → ReviewDto`).

## Deviations

- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.
- The test's mock declaration stays after its stubs go (an unused local or field); `UnusedMapperPattern` removes the constructor argument only.
- "Provably equal" assumes the class under test maps the same, unchanged object the test stubs; the warning says so.

## See also

- [plan](automapper-mock-set-up-plan.md), [modellen](automapper-mock-set-up-modellen.md)
