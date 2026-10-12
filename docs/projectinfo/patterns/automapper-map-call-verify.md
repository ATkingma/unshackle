# Verification: automapper-map-call

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-map-call-plan.md`](automapper-map-call-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-12 by KingmaDoc skill (handmatig, `kingmadoc` niet beschikbaar)                |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs`, `CreateMapPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/MapCall/` en `src/Unshackle.Tests/Proofs/AutoMapper/_Combined/` (die draait `CombinedProofTests.CombinedAutoMapper_Before_BecomesAfter` met de echte `AutoMapperFix`). Unittests: `src/Unshackle.Tests/Core/MapCallPatternTests.cs`.

| REQ   | Oordeel | Bewijs                                                                                                                                       |
| ----- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Gehaald | `existing-method` (`Map<B>`, `Map<A, B>` via `this._mapper`, haakjes om `new Order { .. }`)                                                  |
| REQ-2 | Gehaald | `_Combined/map-call-profile` (using toegevoegd), `_Combined/map-call-global` (met MapFrom), unittest `FixAsync_ProfileInOtherFile_CallsGeneratedMethodWithUsing` |
| REQ-3 | Gehaald | unittest `FixAsync_ProfileInReferencedProject_FollowsAccessibility` (public: Fixed; internal: Manual)                                        |
| REQ-4 | Gehaald | `collection`                                                                                                                                 |
| REQ-5 | Gehaald | `unknown-map`, `manual-profile`                                                                                                              |
| REQ-6 | Gehaald | `maybe-null`                                                                                                                                 |
| REQ-7 | Gehaald | `object-source`, `receiver-call`                                                                                                             |
| REQ-8 | Gehaald | `CreateMap/map-into` (voorheen `map-call`)                                                                                                   |

## Deviations

- `IMapper`-injectie blijft staan (de rij noemde "weg als ongebruikt"): weghalen raakt alle aanroepers; verschoven naar rij 6.
- "Several profiles" heeft geen proof of test; alleen code-review.
- `CombinedProofTests` kreeg een AutoMapper-theorie: een Fixed-proof met alleen `MapCallPattern` compileert niet zonder de profile-rewrite.
- PNG/SVG van de modellen niet gerenderd (geen `kingmadoc`/`d2`); de D2-bronnen staan er.

## Validation results

| Check   | Command                                                     | Result                                                                                                                                                                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                                                                                                                                                                     |
| Tests   | `dotnet test`                                               | 748/783 groen; de 35 falers (`XmlProjectFileStoreTests`, `FixerTests`, `CommandTests.FixExecuteAsync_DirtyTree…`, 2 SampleShop-integratietests) zijn de bekende CRLF-falers van schone main |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38/38)                                                                                                                                                                          |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld                                                                                                                                                 |

## Zie ook

- [automapper-map-call-plan.md](automapper-map-call-plan.md)
- [automapper-map-call-modellen.md](automapper-map-call-modellen.md)
