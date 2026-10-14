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
| REQ-3 | Met | `null-deref`, unittest `Rewrite_NullableAnnotatedReceiver_ReportsManual`                                                                            |
| REQ-4 | Met | `ignore-and-nullable` (`s.Note.ToUpper()` with `#nullable enable`), unit test `Rewrite_ReceiverCheckedForNull_IsMoved`                                |
| REQ-5 | Met | `conversion` (an `.after.cs` since TAAK-247, see [automapper-conversions-verify.md](automapper-conversions-verify.md))                                                                                                                                        |
| REQ-6 | Superseded | since TAAK-245 `collection` is rewritten (`Numbers = source.Numbers?.ToList() ?? []`); see [automapper-nested-maps-verify.md](automapper-nested-maps-verify.md) |
| REQ-7 | Met | `outside-profile`                                                                                                                                   |
| REQ-8 | Met | `resolver-form`, unittests `Rewrite_MapFromConfiguredTwice_ReportsManual`, `Rewrite_MapFromOnReadOnlyMember_ReportsManual`                           |

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
