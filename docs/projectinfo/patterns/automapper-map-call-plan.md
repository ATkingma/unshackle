---
kingmadoc: 1
feature: "automapper-map-call"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/MapCall/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.map-call: mapper.Map<B>(a) → a.ToB()

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Project**   | Unshackle                                                         |
| **Status**    | Approved                                                          |
| **Generated** | 2026-10-12 by KingmaDoc (handmatig, `kingmadoc` niet beschikbaar) |

> Opgesteld uit TAAK-157.

## One-sentence summary

automapper.map-call: `mapper.Map<B>(a)` en `mapper.Map<A, B>(a)` worden `a.ToB()`, de extensiemethode die al bestaat of die `automapper.create-map` (of een ForMember-patroon) voor `CreateMap<A, B>` schrijft.

## Scope (in / out)

**In scope**

- `Map<B>(a)` en `Map<A, B>(a)` van AutoMapper (één argument), aangeroepen op een naam, `this.x` of een keten van namen.
- `ToB` die de aanroepende code al ziet, of die in de broncode van het project staat (dan een `using` voor de namespace).
- `CreateMap<A, B>` in een profile dat de profile-patronen herschrijven, in hetzelfde project of een project waarvan het afhangt.

**Out of scope (Manual of ander patroon)**

- `Map(src, dest)`, `Map` met opties en `ProjectTo`: blijven Manual in `CreateMapPattern` (rijen 5 en 7).
- Collecties (`Map<List<B>>(as)`), bron `object`/`dynamic`, een bron die volgens de nullable-flow null kan zijn.
- Geen of een Manual-profile, meerdere profiles met dezelfde map, een internal methode in een ander project.
- `IMapper`-injectie verwijderen: blijft staan (zie Ontwerp).

## Ontwerp

`MapCallPattern` is een `ISolutionPattern`. De Fixer fixt elk bestand tegen de originele solution: in een ander bestand bestaat `ToB` voor het semantic model nog niet. Het patroon zoekt daarom eerst een bestaande `ToB` (in scope, of in de broncode van de compilatie; dat dekt ook het profile in hetzelfde bestand, dat `CreateMapPattern` dan al herschreven heeft) en anders in de solution elk profile met `CreateMap<A, B>`. Daarvoor roept het `CreateMapPattern.Rewrite` aan en neemt het de methodenaam en toegankelijkheid uit de gegenereerde klasse, zodat die gelijk blijven aan wat de fix echt schrijft.

`CreateMapPattern.Find` laat de aanroepen die `MapCallPattern.IsMapCall` herkent los; de rest van `Map`/`ProjectTo` blijft Manual en houdt met `AllOrNothing` de hele fix tegen.

`IMapper` blijft geïnjecteerd: een constructorparameter weghalen verandert elke aanroeper (DI, tests met `new Service(mapper)`). Opruimen hoort bij rij 6 (`add-automapper`), als ook de registratie verdwijnt.

## Requirements

- **REQ-1**: WHEN `Map<B>(a)` of `Map<A, B>(a)` een zichtbare `ToB()` heeft THE SYSTEM SHALL de aanroep vervangen door `a.ToB()`, met haakjes waar nodig (proof `existing-method`).
- **REQ-2**: WHEN het profile met `CreateMap<A, B>` herschreven wordt THE SYSTEM SHALL `a.ToB()` schrijven en de namespace van de extensieklasse importeren (combined proofs `map-call-profile`, `map-call-global`, unittest `FixAsync_ProfileInOtherFile_CallsGeneratedMethodWithUsing`).
- **REQ-3**: WHEN het profile in een project staat waarvan het aanroepende project afhangt THE SYSTEM SHALL de publieke methode aanroepen en een internal methode Manual melden (unittest `FixAsync_ProfileInReferencedProject_FollowsAccessibility`).
- **REQ-4**: IF bron of doel een collectie is THEN THE SYSTEM SHALL Manual melden (proof `collection`).
- **REQ-5**: IF er geen herschreven `CreateMap<A, B>` en geen `ToB` is THEN THE SYSTEM SHALL Manual melden (proofs `unknown-map`, `manual-profile`).
- **REQ-6**: IF de bron volgens de nullable-flow null kan zijn THEN THE SYSTEM SHALL Manual melden (proof `maybe-null`).
- **REQ-7**: IF de bron `object` of `dynamic` is, of de ontvanger bijwerkingen kan hebben THEN THE SYSTEM SHALL Manual melden (proofs `object-source`, `receiver-call`).
- **REQ-8**: WHEN `Map` met opties of `ProjectTo` voorkomt THE SYSTEM SHALL dat in `CreateMapPattern` Manual laten (geen eigen proof meer: `CreateMap/map-into` is verwijderd sinds `Map(src, dest)` naar `automapper.map-into` ging, zie [automapper-map-into-plan.md](automapper-map-into-plan.md); bijgewerkt in TAAK-159).

## Assumptions

- AutoMapper kiest bij `Map<B>(object)` de map op het runtime-type; hier is het statische type van `a` de bron. Een afgeleid type met een eigen map wordt na de fix anders gemapt.
- Zonder nullable-context is de flow-state `None`: dan wordt herschreven.

## Risks

- `ToB` gooit bij `null` een NullReferenceException waar AutoMapper `null` gaf; code zonder nullable-context die `null` doorgeeft, merkt dat pas bij runtime.
- Per aanroep wordt de solution op profiles doorzocht (alleen bestanden met de tekst `CreateMap`); bij veel aanroepen traag.

## Modellen

Klassendiagram, sequence en beslisboom: [automapper-map-call-modellen.md](automapper-map-call-modellen.md).

## Open questions

- Ongebruikte `IMapper`-injectie: bij rij 6 meenemen.
