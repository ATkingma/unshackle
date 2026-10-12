# Modellen: automapper.map-call

Feiten uit `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs` en `CreateMapPattern.cs`.

> De figuren zijn als D2-bron aangemaakt in `img/`; `kingmadoc`/`d2` waren in deze omgeving niet beschikbaar, dus de PNG/SVG volgen bij de eerstvolgende `kingmadoc render`.

## Figuur 1: klassen

**Figure 1.** `MapCallPattern` als `ISolutionPattern`, met `CreateMapPattern` voor de profiles.

<!-- kingmadoc:diagram img/automapper-map-call-modellen-1.d2 -->
![Figuur 1: klassen](img/automapper-map-call-modellen-1.png)

| Element            | Rol                                                                                                                       |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| `MapCallPattern`   | Id `automapper.map-call`; `Find` via `IsMapCall`; `Rewrite` alleen met een bestaande `ToB`, `RewriteAsync` ook met profiles |
| `IsMapCall`        | `Map` van AutoMapper met één argument en één of twee typeargumenten                                                       |
| `CreateMapPattern` | `Rewrite` levert de gegenereerde extensieklasse; `Find` slaat `IsMapCall`-aanroepen over                                  |
| `Check`            | Ontvanger, brontype, collectie, nullable-flow                                                                             |
| `Existing`         | `ToB` in scope, of in de broncode van de compilatie (met `using`)                                                         |
| `GeneratedAsync`   | Profiles in het project en zijn afhankelijkheden: methodenaam, namespace, internal                                        |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `RewriteAsync` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/automapper-map-call-modellen-2.d2 -->
![Figuur 2: sequence](img/automapper-map-call-modellen-2.png)

| Stap      | Methode in de code                                | Wat gebeurt er                                             |
| --------- | ------------------------------------------------- | ---------------------------------------------------------- |
| Vinden    | `Find`, `IsMapCall`                               | `Map<B>(a)` / `Map<A, B>(a)` van AutoMapper                |
| Toetsen   | `Check`                                           | Ontvanger zonder bijwerkingen, brontype, collectie, null   |
| Bestaand  | `Existing`, `LookupSymbols`, `GetSymbolsWithName` | `ToB` die al bestaat (ook net gegenereerd in dit bestand)  |
| Profiles  | `GeneratedAsync`, `CreateMapPattern.Rewrite`      | Welke `ToB` de fix schrijft en in welke namespace          |
| Schrijven | `Fixed`                                           | `a.ToB()`, haakjes waar nodig, `using` als dat nodig is    |

## Figuur 3: beslisboom

**Figure 3.** De vragen van `RewriteAsync`.

<!-- kingmadoc:diagram img/automapper-map-call-modellen-3.d2 -->
![Figuur 3: beslisboom](img/automapper-map-call-modellen-3.png)

| Blad                              | Voorwaarde                                   | Bewijs                                                               |
| --------------------------------- | -------------------------------------------- | -------------------------------------------------------------------- |
| Manual "not called as mapper.Map" | ontvanger is een aanroep of andere expressie | `receiver-call`                                                      |
| Manual "no type of its own"       | bron is `object` of `dynamic`                | `object-source`                                                      |
| Manual "maps a collection"        | bron of doel is een collectie                | `collection`                                                         |
| Manual "may be null"              | flow-state van de bron is MaybeNull          | `maybe-null`                                                         |
| Manual "No CreateMap"             | geen `ToB`, geen herschreven profile         | `unknown-map`, `manual-profile`                                      |
| Manual "Several profiles"         | meer dan één profile mapt A naar B           | —                                                                    |
| Manual "becomes internal"         | internal `ToB` in een ander project          | unittest                                                             |
| Fixed (`a.ToB()`)                 | alles hierboven gehaald                      | `existing-method`, `map-call-profile`, `map-call-global`, unittests  |

## Zie ook

- [automapper-map-call-plan.md](automapper-map-call-plan.md)
- [automapper-map-call-verify.md](automapper-map-call-verify.md)
- [automapper-create-map-modellen.md](automapper-create-map-modellen.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/MapCall/`
