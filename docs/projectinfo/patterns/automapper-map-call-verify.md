# Verification: automapper-map-call

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-map-call-plan.md`](automapper-map-call-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-12 by KingmaDoc skill (by hand, `kingmadoc` not available)                |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs`, `CreateMapPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/MapCall/` and `src/Unshackle.Tests/Proofs/AutoMapper/_Combined/` (run by `CombinedProofTests.CombinedAutoMapper_Before_BecomesAfter` with the real `AutoMapperFix`). Unit tests: `src/Unshackle.Tests/Core/MapCallPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                                                       |
| ----- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met | `existing-method` (`Map<B>`, `Map<A, B>` via `this._mapper`, parentheses around `new Order { .. }`)                                           |
| REQ-2 | Met | `_Combined/map-call-profile` (using added), `_Combined/map-call-global` (with MapFrom), unit test `FixAsync_ProfileInOtherFile_CallsGeneratedMethodWithUsing` |
| REQ-3 | Met | unittest `FixAsync_ProfileInReferencedProject_FollowsAccessibility` (public: Fixed; internal: Manual)                                        |
| REQ-4 | Superseded by REQ-9 | `collection`                                                                                                                                 |
| REQ-5 | Met | `unknown-map`, `manual-profile`                                                                                                              |
| REQ-6 | Met | `maybe-null`                                                                                                                                 |
| REQ-7 | Met | `object-source`, `receiver-call`                                                                                                             |
| REQ-9 | Met | `_Combined/collection-map-call` (List, array, IEnumerable, IList with `.Count`), `_Combined/collection-nullable-map-call` (non-null source, nullable source), unit test `FixAsync_CollectionWithProfileInOtherFile_SelectsGeneratedMethod` (TAAK-259) |
| REQ-10 | Met | `maybe-null-target` (`Map<B?>`, async `Task<B?>` return, `?.Id` after it), `maybe-null` (var local: Manual), unit test `FixAsync_MaybeNullSource_ConditionalOnlyWhereNullAllowed` (TAAK-259) |
| REQ-8 | Met | `CreateMap/map-into` (voorheen `map-call`)                                                                                                   |

## Deviations

- The `IMapper` injection stays (the row said "removed when unused"): removing it touches all callers; moved to row 6.
- "Several profiles" has no proof or test; only code review.
- `CombinedProofTests` got an AutoMapper theory: a Fixed proof with only `MapCallPattern` does not compile without the profile rewrite.
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## Validation results

| Check   | Command                                                     | Result                                                                                                                                                                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                                                                                                                                                                     |
| Tests   | `dotnet test`                                               | 748/783 green; the 35 failures (`XmlProjectFileStoreTests`, `FixerTests`, `CommandTests.FixExecuteAsync_DirtyTree…`, 2 SampleShop integration tests) are the known CRLF failures of a clean main |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38/38)                                                                                                                                                                          |
| Lint    | _none_                                                      | _not run_: no lint command configured                                                                                                                                                 |

## See also

- [automapper-map-call-plan.md](automapper-map-call-plan.md)
- [automapper-map-call-modellen.md](automapper-map-call-modellen.md)
