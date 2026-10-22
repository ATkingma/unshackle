# Verification: automapper-mock-set-up

|               |                                                                                          |
| ------------- | ---------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-mock-set-up-plan.md`](automapper-mock-set-up-plan.md) |
| **Status**    | Verified (pending the generated row in `docs/patterns.md`, see Deviations)               |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                       |

> Verified by an AI agent against the code of branch `worktree-agent-a2af3d34de85010f0` (TAAK-311); REQ-10..13 (council round 11, A6–A8)
> against branch `worktree-agent-a04c16b800e2dab3f`.

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `_Combined/mocked-stub-removed`; `FixAsync_StubEqualToGeneratedMap_RemovesStubAndMigratesClassUnderTest` (NSubstitute), `FixAsync_MoqStubEqualToGeneratedMap_RemovesStub` (Moq shape, through an in-memory stand-in assembly named `Moq`) |
| REQ-2 | Met     | `FixAsync_StubEqualToGeneratedMap_…` asserts the warning `The stub of IMapper.Map<CustomerDto> at Test.cs:13 is removed` |
| REQ-3 | Met     | theory `FixAsync_StubNotProvablyEqual_KeepsStubAndNamesTest` rows: other constant, `Arg.Any`, object changed after creation |
| REQ-4 | Met     | same theory, row `Received()` (now the A6 reason `the test checks a Map call of the mock at Test.cs:15`) |
| REQ-5 | Met     | theory row `ReferenceEquals` (kept); `FixAsync_RecordComparedWhole_RemovesStub` |
| REQ-6 | Met     | `FixAsync_MapWithOptions_KeepsStub`, `FixAsync_ProfileSetting_KeepsStub`, `FixAsync_OptionsOnAnotherMap_RemovesStub` |
| REQ-7 | Met     | `FixAsync_ClassMapsAPairTheMockDoesNotStub_KeepsStub` (veto and rebuild: `the map Order → OrderDto at Service.cs:… is not stubbed by this mock`) |
| REQ-8 | Met     | `FixAsync_MockWithoutStub_NamesTheTestOfTheClassUnderTest` (`(test: Test.cs:10: the mock stubs no map …)`) |
| REQ-9 | Met     | all earlier AutoMapper proofs and `MapOwnersTests` unchanged and green; blind corpus scan below: same Fixed/Manual per row |
| REQ-10 | Met    | proof `MapCall/mocked-verify` (NSubstitute `Received(1)`, manual "assert the mapped result instead"); theory `FixAsync_VerifyOnMapper_KeepsMockWithTypedCode` (Moq `Verify` with `Times.Once()` and `Times.Never()`, `Code == "VerifyOnMapper"`) |
| REQ-11 | Met    | proof `_Combined/mocked-matcher-stub-removed` (`Arg.Any<Customer>()`); theory `FixAsync_MatcherStubProvablyEqual_RemovesStub` (local and in-place value, warning names the matcher); `FixAsync_MoqMatcherStubProvablyEqual_RemovesStub` (`It.IsAny`) |
| REQ-12 | Met    | theory `FixAsync_MatcherStubNotProvable_KeepsStubWithTypedCode`: two Customers, other constant, value compared whole, a helper call returning a Customer; all `Code == "MockedReturnNotProvable"` |
| REQ-13 | Met    | proof `_Combined/mocked-no-stubs` (discard, Info row); theory `FixAsync_MockWithoutStubsValueNeverRead_MigratesWithInfo` (statement, discard, unread local); theory `FixAsync_MockWithoutStubsValueUsed_KeepsWithTypedCode` (null check, `?.` read, passed on); `FixAsync_MockWithoutStub_NamesTheTestOfTheClassUnderTest` (returned) |

| REQ-14 | Met    | `FixAsync_MatcherStubWithSourceCreatedInPlace_RemovesStub`; theory `FixAsync_MatcherStubWithSourceCreatedInPlaceNotProvable_KeepsStub` (default-constructed `Name` read; non-constant member); theory row of `FixAsync_StubNotProvablyEqual_KeepsStubAndNamesTest` now with two in-place Customers |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 1687 total, 1686 passed, 1 failed: `PatternDocTests.PatternsDoc_MatchesListMarkdown`
  needs the new `automapper.mock-set-up` row in `docs/patterns.md` (a shared file outside this task's claim; the main session adds it).
- Blind corpus, `scan` with the branch tool (read-only copies), AutoMapper rows Manual before (after-298) → after: culinary 70 → 70,
  encryptednotes 6 → 6, estore 22 → 22, msb 29 → 29. No row changed status; the Manual reasons now name the test and why. Blockers found:
  argument matchers (culinary 17, msb 9), `Verify` of the Map call (culinary 7, msb 2), the stubbed object reused by reference in another
  stub or assertion (estore 6, msb 3), a mock without stubs (culinary 3, encryptednotes 1), a map with options (culinary `Review → ReviewDto`).

- Round 11 (A6–A8), 2026-10-22: `dotnet build` 0 warnings; `dotnet test` 2009 total, 2009 passed. Blind corpus (read-only copies, scan
  with the branch tool), Manual before → after: culinary 70 → 70, encryptednotes 6 → 6, estore 22 → 22, msb 29 → 29; no status change.
  The rows now carry codes: `VerifyOnMapper` culinary 11, msb 6; `MockedReturnNotProvable` culinary 11, estore 6, msb 9. Why nothing
  moved: every matcher stub (culinary 17, msb 9) fails "the test member does not build exactly one A in a local" (the source is created
  in place inside a repository stub, or not at all), and most of them also compare the stubbed object whole or set values not read from
  the source; the mocks without stubs (culinary 3, encryptednotes 1) pass the mapped value on (`AddAsync(mapper.Map<UserEntity>(dto))`).

## Deviations

- A8 is stricter than its first text: a member read or null check of the value inside the method also blocks. Council round 12 (A9)
  keeps it that way.
- A7 "every asserted member" is implemented as "every settable member" (as A1): stricter, never weaker.

- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.
- The test's mock declaration stays after its stubs go (an unused local or field); `UnusedMapperPattern` removes the constructor argument only.
- "Provably equal" assumes the class under test maps the same, unchanged object the test stubs; the warning says so.

## See also

- [plan](automapper-mock-set-up-plan.md), [modellen](automapper-mock-set-up-modellen.md)
