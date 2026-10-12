---
kingmadoc: 1
feature: "automapper-for-member-ignore"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/
---

# Feature: automapper.for-member-ignore: ForMember(d => d.X, o => o.Ignore()) → lid weglaten in de ToB-methode

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by KingmaDoc |

> Opgesteld uit TAAK-155.

## One-sentence summary

automapper.for-member-ignore: een `CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore())` in een `Profile` wordt, net als `automapper.create-map`, een `ToB(this A source)`-extensiemethode, waarin het genegeerde lid X niet wordt toegekend.

## Scope (in / out)

**In scope**

- Eén of meer `.ForMember(d => d.X, o => o.Ignore())` direct op `CreateMap<A, B>()` (lambda's met één parameter, `X` een lid van B).
- Een genegeerd lid dat wél een bronlid heeft: ook weglaten (AutoMapper zou het niet vullen).
- Een genegeerd lid dat geen bronlid heeft: voorheen Manual (geen gelijknamig lid), nu overgeslagen.

**Out of scope**

- `MapFrom` (sinds TAAK-156 `automapper.for-member-map-from`) en andere `ForMember`-opties (rij 7): Manual of eigen patroon.
- `ForMember("X", ...)` met stringnaam, geneste leden (`d => d.A.B`), blok-lambda's, `ReverseMap`: Manual.
- Een genegeerd lid dat B niet publiek heeft: Manual.

## Ontwerp

`ForMemberIgnorePattern` erft van `CreateMapPattern` en deelt `Rewrite`: een profile wordt als geheel vervangen door `[profile, <Profile>Extensions]`, dus twee losse patterns op dezelfde klasse zouden elkaar overschrijven. `ForMemberIgnorePattern.Find` claimt de profiles met een `ForMember(..., o => o.Ignore())`, `CreateMapPattern.Find` de rest. De Map/ProjectTo-aanroepen (AllOrNothing) blijven bij `CreateMapPattern`.

## Requirements

- **REQ-1**: WHEN een `CreateMap<A, B>()` een of meer `ForMember(d => d.X, o => o.Ignore())` heeft THE SYSTEM SHALL de ToB-methode zonder X schrijven, ook als A een lid X heeft (proofs `simple`, `mixed`, unittest `Rewrite_IgnoredMemberHasSourceMember_LeavesItOut`).
- **REQ-2**: WHEN een genegeerd lid geen bronlid heeft THE SYSTEM SHALL dat niet als ontbrekend lid melden (proofs `simple`, `mixed`).
- **REQ-3**: IF een lid niet genegeerd is en geen gelijknamig bronlid van hetzelfde type heeft THEN THE SYSTEM SHALL Manual melden, zoals bij `create-map` (proof `unmatched`).
- **REQ-4**: IF een `ForMember` een andere optie heeft dan `Ignore()` (bijv. `MapFrom`), een stringnaam gebruikt of niet direct op `CreateMap` staat (bijv. na `ReverseMap`) THEN THE SYSTEM SHALL Manual melden voor het hele profile (proofs `string-name`, `reverse-map`; MapFrom is sinds TAAK-156 een eigen patroon).
- **REQ-5**: IF het genegeerde lid niet als publiek lid van B bestaat THEN THE SYSTEM SHALL Manual melden (unittest `Rewrite_IgnoredMemberNotPublic_ReportsManual`).
- **REQ-6**: WHEN een profile voorkomt THE SYSTEM SHALL het door precies één van `CreateMapPattern` en `ForMemberIgnorePattern` laten vinden (unittest `Find_ProfileWithOrWithoutIgnore_IsClaimedByOneOfThePatterns`).

## Assumptions

- `Ignore()` laat het bestemmingslid op de standaardwaarde van `new B()`; de nieuwe code doet hetzelfde.
- Een `ForMember` met `Ignore` op een get-only lid verandert niets: het lid wordt toch niet toegekend.

## Risks

- `ForMember` wordt op de AutoMapper-assembly gecontroleerd; een eigen extensiemethode `ForMember` met dezelfde lambda-vorm wordt niet herschreven (symboolcontrole).

## Modellen

Klassendiagram, sequence en beslisboom: [automapper-for-member-ignore-modellen.md](automapper-for-member-ignore-modellen.md).

## Open questions

- Geen.
