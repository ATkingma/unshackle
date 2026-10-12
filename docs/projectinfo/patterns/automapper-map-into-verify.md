# Verification: automapper-map-into

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-map-into-plan.md`](automapper-map-into-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-12 by KingmaDoc skill (handmatig, `kingmadoc` niet beschikbaar)                |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `MapIntoPattern.cs`, `CreateMapPattern.cs`, `MapCallPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/MapInto/` en `_Combined/map-into-profile`. Unittest: `src/Unshackle.Tests/Core/MapIntoPatternTests.cs`.

| REQ   | Oordeel | Bewijs                                                                                                                                               |
| ----- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Gehaald | `existing-method` (`Map(a, b)` en `Map<A, B>(orders[0], dto)`)                                                                                      |
| REQ-2 | Gehaald | `_Combined/map-into-profile` (met Ignore en een tweede map zonder MapInto), unittest `FixAsync_ProfileInOtherFile_WritesMapIntoAndCallsItWithUsing` |
| REQ-3 | Gehaald | `collection`                                                                                                                                         |
| REQ-4 | Gehaald | `unknown-map`                                                                                                                                        |
| REQ-5 | Gehaald | `maybe-null-source`                                                                                                                                  |
| REQ-6 | Gehaald | `maybe-null-destination`                                                                                                                             |
| REQ-7 | Gehaald | `object-source`, `receiver-call`                                                                                                                     |
| REQ-8 | Gehaald | alle bestaande CreateMap-, ForMember- en MapCall-proofs ongewijzigd groen                                                                            |

## Deviations

- Proof `CreateMap/map-into` (Manual) verwijderd: `CreateMapPattern.Find` laat `Map(a, b)` nu over aan dit patroon. REQ-8 van `automapper-map-call-plan.md` noemt die proof nog.
- "Several profiles", "becomes internal" en "already has a member" hebben geen eigen proof; alleen code-review (zelfde logica als `MapCallPattern`).
- PNG/SVG van de modellen niet gerenderd (geen `kingmadoc`/`d2`); de D2-bronnen staan er.

## Validation results

| Check   | Command                                                     | Result                                                                   |
| ------- | ----------------------------------------------------------- | ------------------------------------------------------------------------ |
| Build   | `dotnet build`                                              | passed (0 warnings)                                                      |
| Tests   | `dotnet test`                                               | 756/791 groen; de 35 falers zijn de bekende CRLF-falers van schone main  |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38/38)                                                           |
| Lint    | _geen_                                                      | _not run_                                                                |

## Zie ook

- [automapper-map-into-plan.md](automapper-map-into-plan.md)
- [automapper-map-into-modellen.md](automapper-map-into-modellen.md)
