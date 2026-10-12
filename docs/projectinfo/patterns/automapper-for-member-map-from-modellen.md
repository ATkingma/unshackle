# Modellen: automapper.for-member-map-from

Feiten uit `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs` en `CreateMapPattern.cs`.

> De figuren zijn als D2-bron aangemaakt in `img/`; `kingmadoc`/`d2` waren in deze omgeving niet beschikbaar, dus de PNG/SVG volgen bij de eerstvolgende `kingmadoc render`.

## Figuur 1: klassen

**Figure 1.** `ForMemberMapFromPattern` naast `ForMemberIgnorePattern`, beide subklassen van `CreateMapPattern`.

<!-- kingmadoc:diagram img/automapper-for-member-map-from-modellen-1.d2 -->
![Figuur 1: klassen](img/automapper-for-member-map-from-modellen-1.png)

| Element                   | Rol                                                                                                        |
| ------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `ForMemberMapFromPattern` | Id `automapper.for-member-map-from`; `Find` claimt elk profile met een `ForMember(.., o => o.MapFrom(..))` |
| `CreateMapPattern`        | Gedeelde `Rewrite`; `Find` voor profiles zonder Ignore/MapFrom en voor `Map`/`ProjectTo`                   |
| `ForMemberOption`         | Herkent `d => d.X` met `o.Ignore()` (MapFrom null) of `o.MapFrom(s => expr)` (de lambda)                   |
| `TryParseMap`             | Pelt de `.ForMember`-schakels af; verzamelt `options` (naam naar lambda of null); dubbele naam is geen match |
| `Problem`                 | Per lid van B: genegeerd, MapFrom, of bronlid met zelfde naam en type; levert `(naam, waarde)`             |
| `MapFromProblem`          | Toetst de expressie en vervangt `s` door `source`                                                          |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/automapper-for-member-map-from-modellen-2.d2 -->
![Figuur 2: sequence](img/automapper-for-member-map-from-modellen-2.png)

| Stap      | Methode in de code                                               | Wat gebeurt er                                           |
| --------- | ---------------------------------------------------------------- | -------------------------------------------------------- |
| Vinden    | `Find`, `IsProfileWithMaps`, `UsesForMember`, `IsMapFromCall`    | Profiles met minstens één `MapFrom`-aanroep              |
| Parsen    | `TryParseMap`, `ForMemberOption`, `GetSymbolInfo`                | `CreateMap<A, B>()` plus de opties per lid               |
| Toetsen   | `Problem`, `MapFromProblem`, `GetTypeInfo`, `ClassifyConversion` | Leden, expressie (namen, null-veiligheid, type, collectie) |
| Schrijven | `Extensions`, `ReplaceNode`                                      | Constructor leeg, `<Profile>Extensions` met `X = expr`   |

## Figuur 3: beslisboom

**Figure 3.** De vragen van `Rewrite` die bij MapFrom horen (de overige staan in de create-map- en ignore-modellen).

<!-- kingmadoc:diagram img/automapper-for-member-map-from-modellen-3.d2 -->
![Figuur 3: beslisboom](img/automapper-for-member-map-from-modellen-3.png)

| Blad                           | Voorwaarde                                                   | Bewijs                                     |
| ------------------------------ | ------------------------------------------------------------ | ------------------------------------------ |
| Manual "without options"       | resolver-lambda, stringnaam, generiek, X dubbel              | `resolver-form`, unittest                  |
| Manual "no public setter"      | X heeft geen publieke setter                                 | unittest                                   |
| Manual "not available outside" | expressie gebruikt een instantielid van het profiel          | `outside-profile`                          |
| Manual "which may be null"     | ontvanger is geen `s`/type en niet NotNull                   | `null-deref`, unittest                     |
| Manual "AutoMapper converts"   | type expressie ongelijk aan type X (buiten numeriek/nullable) | `conversion`                               |
| Manual "is a collection"       | X is een collectie                                           | `collection`                               |
| Fixed (`X = expr`)             | alles hierboven gehaald                                      | `simple`, `ignore-and-nullable`, unittests |

## Zie ook

- [automapper-for-member-map-from-plan.md](automapper-for-member-map-from-plan.md)
- [automapper-for-member-map-from-verify.md](automapper-for-member-map-from-verify.md)
- [automapper-for-member-ignore-modellen.md](automapper-for-member-ignore-modellen.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/`
