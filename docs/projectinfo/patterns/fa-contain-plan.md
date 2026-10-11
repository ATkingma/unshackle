---
kingmadoc: 1
feature: "fa-contain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/Contain/
---
# Feature: fa.contain: x.Should().Contain(item|predicate) wordt x.ShouldContain(...), NotContain wordt ShouldNotContain

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-007. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.contain: `Contain`/`NotContain` met één item, predicate of substring wordt `ShouldContain`/`ShouldNotContain`; strings krijgen expliciet `Shouldly.Case.Sensitive`.

## Scope (in / out)

**In scope**

- `x.Should().Contain(item)`, `Contain(predicate)` en `NotContain(...)` (met optionele `because` zonder `becauseArgs`) als losse assertion op `IEnumerable<T>`, arrays, lijsten en `string` (substring).
- `Manual(reden)` voor dictionaries, `Contain(IEnumerable)` (alle items), occurrence-constraints, `becauseArgs`, ketens en gebruikt resultaat.

**Out of scope**

- `ContainKey` (rij 11), `OnlyContain` (rij 12), `ContainSingle`, `ContainInOrder`, `ContainAll/Any`.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `list.Should().Contain(item)` op een collectie staat THE SYSTEM SHALL het herschrijven naar `list.ShouldContain(item)` (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/list.*.cs`, `enumerable.*.cs` voor `IEnumerable<T>` en array).
- **REQ-2**: WHEN `Contain(predicate)` of `NotContain(predicate)` met een lambda staat THE SYSTEM SHALL `ShouldContain(predicate)` resp. `ShouldNotContain(predicate)` geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/predicate.*.cs`).
- **REQ-3**: WHEN `NotContain(item)` op een collectie of string staat THE SYSTEM SHALL `ShouldNotContain` geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/not-contain.*.cs`).
- **REQ-4**: WHEN het subject een `string` is THE SYSTEM SHALL `ShouldContain(x, Shouldly.Case.Sensitive)` geven, omdat Shouldly standaard hoofdletterongevoelig vergelijkt en FluentAssertions hoofdlettergevoelig (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/string.*.cs`).
- **REQ-5**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als `customMessage` overnemen; bij strings na `Case.Sensitive` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/because.*.cs`).
- **REQ-6**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/lambda.*.cs`).
- **REQ-7**: IF `becauseArgs` gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/because-args.*`).
- **REQ-8**: IF het resultaat wordt gebruikt (`.And`/`.Which`, variabele) THEN THE SYSTEM SHALL Manual geven (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/chain.*`, `value-used.*`).
- **REQ-9**: IF de assertion zelf een vervolg op een andere assertion is THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/and-chain.*`).
- **REQ-10**: IF het subject een dictionary is THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/dictionary.*`).
- **REQ-11**: IF `Contain(IEnumerable)` (alle items) of een occurrence-constraint wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/items.*`, `occurrence.*`).

## Assumptions

- Shouldly 4.3.0 heeft `ShouldContain<T>(IEnumerable<T>, T, string? customMessage)` en `(…, Expression<Func<T,bool>>, …)`; bewezen door de compileercheck van de after-proofs.
- De overload wordt bepaald via het gedeclareerde parametertype van het FA-symbool (type-parameter, `string`, `Expression`, anders Manual).
- De `Fixer` beheert `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- Zonder `Case.Sensitive` zou de herschrijving stil versoepelen (hoofdletters negeren); daarom altijd expliciet toegevoegd.
- Foutmelding krijgt "Additional Info:" in plaats van FA's "because"-voorvoegsel: gedocumenteerd verschil.
- Niet-generieke `IEnumerable` valt onder Manual.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-contain-modellen.md](fa-contain-modellen.md).

## Open questions

- Geen.
