# Modellen: automapper.create-map

Feiten uit `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` en `AutoMapperFix.cs`.

## Figuur 1: klassen

**Figure 1.** `AutoMapperFix`, `CreateMapPattern` en wat hij herkent en schrijft.

<!-- kingmadoc:diagram img/automapper-create-map-modellen-1.d2 -->
![Figuur 1: klassen](img/automapper-create-map-modellen-1.png)

| Element            | Rol                                                                                     |
| ------------------ | --------------------------------------------------------------------------------------- |
| `AutoMapperFix`    | Fix `automapper`, `AllOrNothing`: één Manual en de solution blijft zoals hij was        |
| `CreateMapPattern` | Vindt profiles en `Map`/`ProjectTo`-aanroepen, schrijft `<Profile>Extensions`           |
| `Problem`          | Reden waarom een map geen gewone toekenningen is, anders de lijst gelijknamige leden    |
| `Effective`        | Echte toegankelijkheid van A en B (geneste en generieke typen) → `public` of `internal` |
| `Extensions`       | Bouwt de statische klasse als tekst met de inspringing en regeleinden van het bestand   |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/automapper-create-map-modellen-2.d2 -->
![Figuur 2: sequence](img/automapper-create-map-modellen-2.png)

| Stap      | Methode in de code                           | Wat gebeurt er                                            |
| --------- | -------------------------------------------- | --------------------------------------------------------- |
| Vinden    | `Find`, `IsProfile`, `GetSymbolInfo`         | Profiles met een niet-lege constructor; `Map`/`ProjectTo` |
| Toetsen   | `Problem`, `Effective`, `GetSymbolsWithName` | Leden, typen, constructor, toegankelijkheid, naamconflict |
| Schrijven | `Extensions`, `ReplaceNode`                  | Constructor leeg, statische klasse direct na het profile  |
| Fixer     | `AllOrNothing`                               | Eén Manual in de solution → niets toegepast               |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Rewrite`.

<!-- kingmadoc:diagram img/automapper-create-map-modellen-3.d2 -->
![Figuur 3: beslisboom](img/automapper-create-map-modellen-3.png)

| Blad                                             | Voorwaarde                                 | Bewijs                     |
| ------------------------------------------------ | ------------------------------------------ | -------------------------- |
| Manual "not migrated yet"                        | `ProjectTo`, `Map(src, dest)` van AutoMapper | `map-into`                 |
| Manual "nested, generic, partial…"               | profile geen eenvoudige top-level klasse   | n.v.t. (zeldzaam)          |
| Manual "not a CreateMap without options"         | `ReverseMap`, `ForMember`, ander statement | `options`                  |
| Manual "no public parameterless constructor"     | positioneel record, abstract               | `constructor`, unittest    |
| Manual "no member with the same name"            | flattening, `GetX()`, andere hoofdletters  | `flattening`, unittest     |
| Manual "AutoMapper converts" / "is a collection" | ander type, lijst of array                 | `conversion`, `collection` |
| Manual "no public setter"                        | get-only bestemmingslid met bronlid        | unittest                   |
| Fixed                                            | alles hierboven nee                        | `simple`, `internal-types` |

## Zie ook

- [automapper-create-map-plan.md](automapper-create-map-plan.md)
- [automapper-create-map-verify.md](automapper-create-map-verify.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/`
