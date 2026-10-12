---
kingmadoc: 1
feature: "automapper-add-automapper"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/AddAutoMapper/
---

# Feature: automapper.add-automapper: services.AddAutoMapper(...) → regel weg

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Project**   | Unshackle                                                         |
| **Status**    | Approved                                                          |
| **Generated** | 2026-10-12 by KingmaDoc (handmatig, `kingmadoc` niet beschikbaar) |

> Opgesteld uit TAAK-159.

## One-sentence summary

automapper.add-automapper: de statement `services.AddAutoMapper(...);` verdwijnt, maar alleen als niets in de solution nog `IMapper`, `IMapperBase`, `IRuntimeMapper` of `IConfigurationProvider` noemt.

## Scope (in / out)

**In scope**

- Elke `AddAutoMapper`-overload uit de AutoMapper-assembly als losse statement.
- Een parameter die alleen aan `AddAutoMapper` werd doorgegeven krijgt `_ = p;` (signatuur en aanroepers blijven, geen IDE0060).

**Out of scope (Manual)**

- Een AutoMapper-service wordt nog gebruikt (veld, constructorparameter, `GetRequiredService<IMapper>()`), ook in een ander project (bv. tests).
- Het resultaat van `AddAutoMapper` wordt gebruikt (expression body, keten, return).

## Ontwerp

`AddAutoMapperPattern` is een `ISolutionPattern`, net als `AddMediatRPattern`. `RewriteAsync` zoekt met `SymbolFinder.FindReferencesAsync` verwijzingen naar de vier service-typen in de hele solution; de sync `Rewrite` kijkt alleen in de eigen compilatie. Anders dan AddMediatR (dat naar `AddMediator` wordt omgezet) is er geen vervanger: Unshackle genereert gewone extensiemethoden, dus de registratie is overbodig zodra niemand een mapper uit DI vraagt.

## Requirements

- **REQ-1**: WHEN `AddAutoMapper(...)` een losse statement is en geen mapper-service meer gebruikt wordt THE SYSTEM SHALL de statement verwijderen (proof `simple`).
- **REQ-2**: WHEN een parameter alleen aan `AddAutoMapper` werd doorgegeven THE SYSTEM SHALL `_ = p;` laten staan (proof `parameter`).
- **REQ-3**: IF `IMapper` (of een andere AutoMapper-service) nog ergens in de solution gebruikt wordt THEN THE SYSTEM SHALL Manual melden met de plek (proof `mapper-injected`).
- **REQ-4**: IF het resultaat van `AddAutoMapper` gebruikt wordt THEN THE SYSTEM SHALL Manual melden (proof `result-used`).

## Assumptions

- `MapCallPattern` laat `IMapper`-injectie staan (TAAK-157); na handmatig opruimen verdwijnt de registratie bij een volgende run.
- De lege Profiles blijven en houden het AutoMapper-pakket nodig; het pakket gaat pas in een latere rij weg.

## Risks

- `FindReferencesAsync` per service per aanroep; bij grote solutions trager, maar `AddAutoMapper` komt zelden voor.

## Modellen

Klassendiagram, sequence en beslisboom: [automapper-add-automapper-modellen.md](automapper-add-automapper-modellen.md).

## Open questions

- Geen.
