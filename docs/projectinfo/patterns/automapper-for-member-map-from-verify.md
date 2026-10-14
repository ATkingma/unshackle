# Verification: automapper-for-member-map-from

|               |                                                                                                              |
| ------------- | ------------------------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-for-member-map-from-plan.md`](automapper-for-member-map-from-plan.md) |
| **Status**    | Verified                                                                                                     |
| **Generated** | 2026-10-12 by KingmaDoc skill (by hand, `kingmadoc` not available)                                      |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs`, `CreateMapPattern.cs`, `ForMemberIgnorePattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/`. Unittests: `src/Unshackle.Tests/Core/ForMemberMapFromPatternTests.cs`, `ForMemberIgnorePatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                                                              |
| ----- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met | `simple` (arithmetic, string concatenation, int to long), `ignore-and-nullable`, unit test `Rewrite_MapFromWithRenamedParameter_UsesSourceInTheExpression` |
| REQ-2 | Met | `ignore-and-nullable`, unittest `Find_ProfileWithOrWithoutOptions_IsClaimedByOneOfThePatterns`                                                      |
| REQ-3 | Met | `may-be-null-call` (a call on a possibly-null receiver), ProjectTo unit test `FixAsync_MapFromThroughMaybeNullMember_ProjectionIsManual` (plain chains: REQ-9) |
| REQ-4 | Met | `ignore-and-nullable` (`s.Note.ToUpper()` with `#nullable enable`), unit test `Rewrite_ReceiverCheckedForNull_IsMoved`                                |
| REQ-5 | Met | `conversion` (an `.after.cs` since TAAK-247, see [automapper-conversions-verify.md](automapper-conversions-verify.md))                                                                                                                                        |
| REQ-6 | Superseded | since TAAK-245 `collection` is rewritten (`Numbers = source.Numbers?.ToList() ?? []`); see [automapper-nested-maps-verify.md](automapper-nested-maps-verify.md) |
| REQ-7 | Met | `outside-profile`                                                                                                                                   |
| REQ-8 | Met | resolver form moved to `ValueResolver/lambda` (TAAK-254), unittests `Rewrite_MapFromConfiguredTwice_ReportsManual`, `Rewrite_MapFromOnReadOnlyMember_ReportsManual`                           |
| REQ-9 | Met | `may-be-null` (`source.Customer?.Name`, `source.Customer?.Age ?? default`), `reverse-map-may-be-null`, `_Combined/may-be-null-profile`, unit test `Rewrite_NullableAnnotatedReceiver_UsesNullConditional` (`source.Customer?.Name!`) |
| REQ-10 | Met | `_Combined/projection-parameters` (`int userId = 0;` becomes `ToSeriesDto(this Series source, int userId = 0)`; `Map` call keeps `ToSeriesDto()`) |

## Deviations

- No own `Rewrite`: shared with `CreateMapPattern` (a profile is replaced as a whole).
- The proof `map-from` of `ForMemberIgnore` (Manual) was dropped; MapFrom is now this pattern.
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## Validation results

| Check   | Command                                                       | Result                                                                                                                                              |
| ------- | ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Build   | `dotnet build -p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507` | passed (0 warnings)                                                                                                                                 |
| Tests   | `dotnet test` (same options)                                  | 736/771 green; ProofTests and pattern tests 568/568; the 35 failures (`XmlProjectFileStoreTests`, `FixerTests`, `CommandTests`, 2 SampleShop integration tests) are CRLF-related and unrelated to this pattern |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx`   | _not run_                                                                                                                                           |
| Lint    | _none_                                                        | _not run_: no lint command configured                                                                                                             |

## See also

- [automapper-for-member-map-from-plan.md](automapper-for-member-map-from-plan.md)
- [automapper-for-member-map-from-modellen.md](automapper-for-member-map-from-modellen.md)
