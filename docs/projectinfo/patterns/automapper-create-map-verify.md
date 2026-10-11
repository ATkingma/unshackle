# Verification: automapper-create-map

|               |                                                                                            |
| ------------- | ------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-create-map-plan.md`](automapper-create-map-plan.md) |
| **Status**    | Verified                                                                                   |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                        |

> Verified by an AI agent against the code at uncommitted changes (basis 5bca3cb).

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, `AutoMapperFix.cs`, registratie in `src/Unshackle.Interface/Cli.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/`. Unittests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ   | Oordeel | Bewijs                                                                        |
| ----- | ------- | ----------------------------------------------------------------------------- |
| REQ-1 | Gehaald | `simple` (properties, `init`, veld, struct-bestemming, get-only overgeslagen) |
| REQ-2 | Gehaald | `internal-types`                                                              |
| REQ-3 | Gehaald | `options`                                                                     |
| REQ-4 | Gehaald | `flattening`, unittest hoofdletters                                           |
| REQ-5 | Gehaald | `conversion`, `collection`, unittest `int[]`                                  |
| REQ-6 | Gehaald | `constructor`, unittest abstract                                              |
| REQ-7 | Gehaald | `map-call`                                                                    |

Doortest SampleShop (kopie in `/mnt/dikkeschijf/unshackle-corpus/t146/`): 0 Fixed, 10 Manual (8× `Map`, 2 profiles met opties), 0 bestanden gewijzigd (AllOrNothing); de kopie bouwt en 38/38 tests zijn groen.

## Deviations

- Parameternaam `source` in plaats van `a` uit de wachtrij: voorkomt keywords als parameternaam.
- `Fixer.ReplaceWithTrivia` laat nu lege regels staan die een pattern vóór een extra node zet (anders kwam de nieuwe klasse zonder witregel direct na het profile).
- Het lege profile blijft staan en het pakket ook (zie plan, Out of scope).

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 716/716 groen                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen                             |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld |

## Zie ook

- [automapper-create-map-plan.md](automapper-create-map-plan.md)
- [automapper-create-map-modellen.md](automapper-create-map-modellen.md)
