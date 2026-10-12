# Verification: automapper-for-member-map-from

|               |                                                                                                              |
| ------------- | ------------------------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-for-member-map-from-plan.md`](automapper-for-member-map-from-plan.md) |
| **Status**    | Verified                                                                                                     |
| **Generated** | 2026-10-12 by KingmaDoc skill (handmatig, `kingmadoc` niet beschikbaar)                                      |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs`, `CreateMapPattern.cs`, `ForMemberIgnorePattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/`. Unittests: `src/Unshackle.Tests/Core/ForMemberMapFromPatternTests.cs`, `ForMemberIgnorePatternTests.cs`.

| REQ   | Oordeel | Bewijs                                                                                                                                              |
| ----- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Gehaald | `simple` (rekenen, string-concatenatie, int naar long), `ignore-and-nullable`, unittest `Rewrite_MapFromWithRenamedParameter_UsesSourceInTheExpression` |
| REQ-2 | Gehaald | `ignore-and-nullable`, unittest `Find_ProfileWithOrWithoutOptions_IsClaimedByOneOfThePatterns`                                                      |
| REQ-3 | Gehaald | `null-deref`, unittest `Rewrite_NullableAnnotatedReceiver_ReportsManual`                                                                            |
| REQ-4 | Gehaald | `ignore-and-nullable` (`s.Note.ToUpper()` met `#nullable enable`), unittest `Rewrite_ReceiverCheckedForNull_IsMoved`                                 |
| REQ-5 | Gehaald | `conversion`                                                                                                                                        |
| REQ-6 | Gehaald | `collection`                                                                                                                                        |
| REQ-7 | Gehaald | `outside-profile`                                                                                                                                   |
| REQ-8 | Gehaald | `resolver-form`, unittests `Rewrite_MapFromConfiguredTwice_ReportsManual`, `Rewrite_MapFromOnReadOnlyMember_ReportsManual`                           |

## Deviations

- Geen eigen `Rewrite`: gedeeld met `CreateMapPattern` (een profile wordt als geheel vervangen).
- De proof `map-from` van `ForMemberIgnore` (Manual) is vervallen; MapFrom is nu dit patroon.
- PNG/SVG van de modellen niet gerenderd (geen `kingmadoc`/`d2`); de D2-bronnen staan er.

## Validation results

| Check   | Command                                                       | Result                                                                                                                                              |
| ------- | ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Build   | `dotnet build -p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507` | passed (0 warnings)                                                                                                                                 |
| Tests   | `dotnet test` (zelfde opties)                                 | 736/771 groen; ProofTests en pattern-tests 568/568; de 35 falers (`XmlProjectFileStoreTests`, `FixerTests`, `CommandTests`, 2 SampleShop-integratietests) zijn CRLF-gerelateerd en staan los van dit patroon |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx`   | _not run_                                                                                                                                           |
| Lint    | _geen_                                                        | _not run_: geen lint-commando ingesteld                                                                                                             |

## Zie ook

- [automapper-for-member-map-from-plan.md](automapper-for-member-map-from-plan.md)
- [automapper-for-member-map-from-modellen.md](automapper-for-member-map-from-modellen.md)
