---
kingmadoc: 1
feature: "automapper-map-into"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapIntoPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/MapInto/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.map-into: mapper.Map(a, b) → a.MapInto(b)

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Project**   | Unshackle                                                         |
| **Status**    | Approved                                                          |
| **Generated** | 2026-10-12 by KingmaDoc (handmatig, `kingmadoc` niet beschikbaar) |

> Opgesteld uit TAAK-158.

## One-sentence summary

automapper.map-into: `mapper.Map(a, b)` (`Map<A, B>(A, B)`) wordt `a.MapInto(b)`, een extensiemethode die de profile-rewrite naast `ToB` schrijft voor elke map die zo'n aanroep in de solution gebruikt.

## Scope (in / out)

**In scope**

- `Map(a, b)` en `Map<A, B>(a, b)` van AutoMapper (tweede parameter van type `B`, geen opties-lambda), op een naam of keten van namen.
- `MapInto(this A, B)` die al bestaat (in scope of in de broncode van het project).
- `CreateMap<A, B>` in een profile dat de profile-patronen herschrijven: `CreateMapPattern.RewriteAsync` voegt dan `B MapInto(this A source, B destination)` toe met dezelfde toewijzingen als `ToB` (Ignore weggelaten, MapFrom-expressies) en `return destination;`.

**Out of scope (Manual)**

- Collecties, bron of doel `object`/`dynamic`, bron of doel MaybeNull, ontvanger met bijwerkingen.
- Geen of een Manual-profile, meerdere profiles, internal `MapInto` in een ander project, een bestaand member `MapInto` op `A`.
- `Map` met opties en `ProjectTo`: blijven Manual in `CreateMapPattern` (rij 7).

## Ontwerp

`MapIntoPattern` is een `ISolutionPattern` en volgt `MapCallPattern`: eerst een bestaande `MapInto`, anders `MapCallPattern.GeneratedAsync` (zelfde profile-zoektocht; namespace en internal uit de gegenereerde klasse). `CreateMapPattern` werd ook een `ISolutionPattern`: `RewriteAsync` haalt via `MapIntoPattern.TargetsAsync` de paren (A, B) op die een `Map(a, b)` in de solution gebruikt en schrijft alleen daarvoor `MapInto`, zodat bestaande uitvoer gelijk blijft. De `MapInto`-methoden komen na alle `ToB`-methoden (`GeneratedAsync` koppelt methode i aan CreateMap i). `MapInto` geeft `destination` terug, zodat een gebruikt resultaat (`x = mapper.Map(a, b)`) blijft werken.

## Requirements

- **REQ-1**: WHEN `Map(a, b)` een zichtbare `MapInto(this A, B)` heeft THE SYSTEM SHALL de aanroep vervangen door `a.MapInto(b)` (proof `MapInto/existing-method`).
- **REQ-2**: WHEN het profile met `CreateMap<A, B>` herschreven wordt en de solution `Map(a, b)` bevat THE SYSTEM SHALL naast `ToB` een `MapInto` schrijven en de aanroep `a.MapInto(b)` maken, met `using` waar nodig (combined proof `map-into-profile`, unittest `FixAsync_ProfileInOtherFile_WritesMapIntoAndCallsItWithUsing`).
- **REQ-3**: IF bron of doel een collectie is THEN THE SYSTEM SHALL Manual melden (proof `collection`).
- **REQ-4**: IF er geen herschreven `CreateMap<A, B>` en geen `MapInto` is THEN THE SYSTEM SHALL Manual melden (proof `unknown-map`).
- **REQ-5**: IF de bron volgens de nullable-flow null kan zijn THEN THE SYSTEM SHALL Manual melden (proof `maybe-null-source`).
- **REQ-6**: IF het doel null kan zijn THEN THE SYSTEM SHALL Manual melden; AutoMapper maakt dan een nieuw object (proof `maybe-null-destination`).
- **REQ-7**: IF bron of doel `object`/`dynamic` is, of de ontvanger bijwerkingen kan hebben THEN THE SYSTEM SHALL Manual melden (proofs `object-source`, `receiver-call`).
- **REQ-8**: WHEN geen `Map(a, b)` de map gebruikt THE SYSTEM SHALL geen `MapInto` schrijven (bestaande CreateMap-, ForMember- en MapCall-proofs ongewijzigd).

## Assumptions

- AutoMapper kiest de map op het runtime-type; hier op de statische typen.
- `MapInto` kopieert dezelfde members als `ToB`; get-only members die de bron heeft zijn al Manual in `CreateMapPattern`.

## Risks

- Elke profile-rewrite met solution doorzoekt alle bestanden met de tekst `Map` naar `Map(a, b)`; bij grote solutions traag.
- Een struct als doel: AutoMapper en `MapInto` geven beide een kopie terug; gedrag gelijk, maar ongebruikelijk.

## Modellen

Klassendiagram, sequence en beslisboom: [automapper-map-into-modellen.md](automapper-map-into-modellen.md).

## Open questions

- Geen.
