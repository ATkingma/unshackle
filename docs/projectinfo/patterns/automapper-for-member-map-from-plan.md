---
kingmadoc: 1
feature: "automapper-for-member-map-from"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
---

# Feature: automapper.for-member-map-from: ForMember(d => d.X, o => o.MapFrom(s => expr)) → X = expr in de ToB-methode

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by KingmaDoc |

> Opgesteld uit TAAK-156.

## One-sentence summary

automapper.for-member-map-from: een `CreateMap<A, B>().ForMember(d => d.X, o => o.MapFrom(s => expr))` in een `Profile` wordt, net als `automapper.create-map`, een `ToB(this A source)`-extensiemethode met `X = expr`, waarin de lambda-parameter `s` is vervangen door `source`.

## Scope (in / out)

**In scope**

- `.ForMember(d => d.X, o => o.MapFrom(s => expr))` direct op `CreateMap<A, B>()`, ook samen met `Ignore()` en andere MapFrom-schakels in dezelfde keten of hetzelfde profiel.
- `expr` zonder conversie: type gelijk aan het type van X, of impliciet numeriek/nullable.
- `expr` die niet kan gooien: ontvangers van `.lid` of `[ ]` zijn `s`, een type, of volgens de nullable-flow niet null.

**Out of scope (Manual)**

- `MapFrom` met een resolver-lambda van twee of drie parameters, een stringnaam, een blok-lambda of een generieke `MapFrom<T>`.
- Expressies die iets uit het profiel gebruiken (`this`, instantievelden, statics van het profiel) of een naam `source` die al iets anders is.
- Expressies die een mogelijk-null-ontvanger dereferencen, of waar AutoMapper converteert (type verschilt), of een collectie (AutoMapper kopieert).
- X dat B niet publiek schrijfbaar heeft; X twee keer geconfigureerd.

## Ontwerp

`ForMemberMapFromPattern` erft van `CreateMapPattern` (zoals `ForMemberIgnorePattern`) en deelt `Rewrite`: een profile wordt als geheel vervangen door `[profile, <Profile>Extensions]`, dus Ignore en MapFrom in één profile moeten één rewrite delen. De `Find`-verdeling is exclusief: een profile met een MapFrom-aanroep (welke vorm ook) gaat naar dit pattern, anders met `Ignore()` naar `ForMemberIgnorePattern`, anders naar `CreateMapPattern`. De ledenlijst van de rewrite bevat nu `(naam, waarde)`: `source.X` of de herschreven expressie, in de volgorde van de leden van B.

AutoMapper voert een `MapFrom`-expressie null-veilig uit (een `NullReferenceException` geeft de standaardwaarde) en converteert typen; een gewone toekenning doet geen van beide. Daarom wordt alleen een expressie verplaatst die niet kan gooien en geen conversie nodig heeft; de nullable-flow van Roslyn (`TypeInfo.Nullability`) bepaalt of een ontvanger niet-null is. In code zonder nullable-context is elke dereferentie dus Manual.

## Requirements

- **REQ-1**: WHEN een `CreateMap<A, B>()` een `ForMember(d => d.X, o => o.MapFrom(s => expr))` heeft THE SYSTEM SHALL de ToB-methode `X = expr` laten toekennen met `s` vervangen door `source` (proofs `simple`, `ignore-and-nullable`, unittest `Rewrite_MapFromWithRenamedParameter_UsesSourceInTheExpression`).
- **REQ-2**: WHEN een profile Ignore en MapFrom combineert THE SYSTEM SHALL beide in dezelfde ToB-methode verwerken en het profile door precies één pattern laten vinden (proof `ignore-and-nullable`, unittest `Find_ProfileWithOrWithoutOptions_IsClaimedByOneOfThePatterns`).
- **REQ-3**: IF de expressie een ontvanger dereferenceert die null kan zijn THEN THE SYSTEM SHALL Manual melden (proof `null-deref`, unittest `Rewrite_NullableAnnotatedReceiver_ReportsManual`).
- **REQ-4**: WHEN een ontvanger volgens de nullable-flow niet null is (non-nullable lid, of eerst op null getest) THE SYSTEM SHALL de expressie verplaatsen (proof `ignore-and-nullable`, unittest `Rewrite_ReceiverCheckedForNull_IsMoved`).
- **REQ-5**: IF het type van de expressie niet gelijk is aan dat van X (buiten impliciet numeriek of nullable) THEN THE SYSTEM SHALL Manual melden (proof `conversion`).
- **REQ-6**: IF X een collectie is THEN THE SYSTEM SHALL Manual melden (proof `collection`).
- **REQ-7**: IF de expressie iets gebruikt dat buiten het profiel niet bestaat (instantieveld, `this`) THEN THE SYSTEM SHALL Manual melden (proof `outside-profile`).
- **REQ-8**: IF de MapFrom-vorm niet ondersteund is (resolver-lambda, X twee keer, X zonder publieke setter) THEN THE SYSTEM SHALL Manual melden (proof `resolver-form`, unittests `Rewrite_MapFromConfiguredTwice_ReportsManual`, `Rewrite_MapFromOnReadOnlyMember_ReportsManual`).

## Assumptions

- `MapFrom(s => expr)` met één parameter is de `Expression<Func<TSource, TMember>>`-overload van AutoMapper; expression trees staan geen `?.` toe, dus die komt niet voor.
- Een niet-nullable referentie is volgens het nullable-contract niet null; wie dat contract niet bewaakt, krijgt hier een te optimistische herschrijving.

## Risks

- Corpuscode zonder nullable-context krijgt vaker Manual (elke dereferentie van een referentietype), bewust: een stille gedragswijziging is erger.
- Een lege bron (`s` zelf null) geeft in `ToB` een NullReferenceException; vgl. rij 1.

## Modellen

Klassendiagram, sequence en beslisboom: [automapper-for-member-map-from-modellen.md](automapper-for-member-map-from-modellen.md).

## Open questions

- Geen.
