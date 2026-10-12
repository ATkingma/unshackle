# Verification: automapper-for-member-ignore

|               |                                                                                                          |
| ------------- | -------------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-for-member-ignore-plan.md`](automapper-for-member-ignore-plan.md) |
| **Status**    | Verified                                                                                                 |
| **Generated** | 2026-10-12 by KingmaDoc skill (handmatig, `kingmadoc` niet beschikbaar)                                  |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs`, `CreateMapPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/`. Unittests: `src/Unshackle.Tests/Core/ForMemberIgnorePatternTests.cs`.

| REQ   | Oordeel | Bewijs                                                                                                                                  |
| ----- | ------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Gehaald | `simple` (Ignore met en zonder bronlid, twee ForMember-schakels), `mixed`, unittest `Rewrite_IgnoredMemberHasSourceMember_LeavesItOut` |
| REQ-2 | Gehaald | `simple`, `mixed`                                                                                                                       |
| REQ-3 | Gehaald | `unmatched`                                                                                                                             |
| REQ-4 | Gehaald | `map-from`, `string-name`, `reverse-map`                                                                                                |
| REQ-5 | Gehaald | unittest `Rewrite_IgnoredMemberNotPublic_ReportsManual`                                                                                 |
| REQ-6 | Gehaald | unittest `Find_ProfileWithOrWithoutIgnore_IsClaimedByOneOfThePatterns`                                                                  |

## Deviations

- Geen eigen `Rewrite`: `ForMemberIgnorePattern` erft die van `CreateMapPattern` (een profile wordt als geheel vervangen, zie plan, Ontwerp).
- De PNG/SVG van de modellen zijn niet gerenderd (geen `kingmadoc`/`d2` beschikbaar); de D2-bronnen staan er.

## Validation results

| Check   | Command                                                       | Result                                                                                                                                                                                              |
| ------- | ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Build   | `dotnet build -p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507` | passed (0 warnings); zonder die opties faalt restore in deze omgeving op een tweede NuGet-bron (`afas`)                                                                                             |
| Tests   | `dotnet test` (zelfde opties)                                 | 723/759 groen; ProofTests en pattern-tests 556/556 groen; de 36 falers (`FixerTests`, `XmlProjectFileStoreTests`, `CommandTests`, SampleShop-integratie) liggen buiten dit patroon (regeleinden en NuGet-bron in deze Windows-omgeving) |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx`   | _not run_                                                                                                                                                                                           |
| Lint    | _geen_                                                        | _not run_: geen lint-commando ingesteld                                                                                                                                                             |

## Zie ook

- [automapper-for-member-ignore-plan.md](automapper-for-member-ignore-plan.md)
- [automapper-for-member-ignore-modellen.md](automapper-for-member-ignore-modellen.md)
