# Modellen: automapper.for-member-ignore

Feiten uit `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs` en `CreateMapPattern.cs`.

> De figuren zijn als D2-bron aangemaakt in `img/`; `kingmadoc`/`d2` waren in deze omgeving niet beschikbaar, dus de PNG/SVG volgen bij de eerstvolgende `kingmadoc render`.

## Figuur 1: klassen

**Figure 1.** `ForMemberIgnorePattern` als subklasse van `CreateMapPattern`.

<!-- kingmadoc:diagram img/automapper-for-member-ignore-modellen-1.d2 -->
![Figuur 1: klassen](img/automapper-for-member-ignore-modellen-1.png)

| Element                  | Rol                                                                                           |
| ------------------------ | --------------------------------------------------------------------------------------------- |
| `ForMemberIgnorePattern` | Id `automapper.for-member-ignore`; `Find` claimt profiles met `ForMember(.., o => o.Ignore())` |
| `CreateMapPattern`       | Basis: `Rewrite` (gedeeld), `Find` voor profiles zonder Ignore en voor `Map`/`ProjectTo`      |
| `TryParseMap`            | Pelt `.ForMember(...)`-schakels van `CreateMap<A, B>()`; verzamelt de genegeerde ledennamen   |
| `IgnoredMember`          | Herkent `d => d.X` + `o => o.Ignore()`, geeft `X`, anders null                                |
| `Problem`                | Slaat genegeerde leden over, controleert de rest                                              |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/automapper-for-member-ignore-modellen-2.d2 -->
![Figuur 2: sequence](img/automapper-for-member-ignore-modellen-2.png)

| Stap      | Methode in de code                                 | Wat gebeurt er                                                  |
| --------- | -------------------------------------------------- | --------------------------------------------------------------- |
| Vinden    | `Find`, `IsProfileWithMaps`, `UsesForMemberIgnore` | Profiles met minstens één `ForMember(.., o => o.Ignore())`      |
| Parsen    | `TryParseMap`, `IgnoredMember`, `GetSymbolInfo`    | `CreateMap<A, B>()` plus de genegeerde ledennamen               |
| Toetsen   | `Problem`, `Effective`, `GetSymbolsWithName`       | Genegeerd lid bestaat; overige leden, typen, toegankelijkheid   |
| Schrijven | `Extensions`, `ReplaceNode`                        | Constructor leeg, `<Profile>Extensions` zonder genegeerde leden |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Rewrite` die bij Ignore horen (de overige staan in het create-map-model).

<!-- kingmadoc:diagram img/automapper-for-member-ignore-modellen-3.d2 -->
![Figuur 3: beslisboom](img/automapper-for-member-ignore-modellen-3.png)

| Blad                                        | Voorwaarde                                                  | Bewijs                                   |
| ------------------------------------------- | ----------------------------------------------------------- | ---------------------------------------- |
| Manual "not a CreateMap without options"    | `MapFrom`, stringnaam, geneste lid of `ReverseMap` in keten | `string-name`, `reverse-map` |
| Manual "does not have as a public property" | genegeerd lid bestaat niet publiek op B                     | unittest                                 |
| Manual "no member with the same name"       | niet-genegeerd lid zonder bronlid                           | `unmatched`                              |
| Fixed (lid weggelaten)                      | genegeerd lid, met of zonder bronlid                        | `simple`, `mixed`, unittest              |

## Zie ook

- [automapper-for-member-ignore-plan.md](automapper-for-member-ignore-plan.md)
- [automapper-for-member-ignore-verify.md](automapper-for-member-ignore-verify.md)
- [automapper-create-map-modellen.md](automapper-create-map-modellen.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/`
