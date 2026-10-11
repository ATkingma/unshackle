---
kingmadoc: 1
feature: "fa-only-contain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/OnlyContain/
---
# Feature: fa.only-contain: x.Should().OnlyContain(pred) wordt x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-015.

## One-sentence summary

fa.only-contain: `x.Should().OnlyContain(pred)` wordt twee statements, `x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);`, omdat FluentAssertions op een lege collectie faalt en Shouldly niet.

## Scope (in / out)

**In scope**

- `OnlyContain(predicate)` (optioneel met `because` zonder `becauseArgs`) op een statement in een blok, met een eenvoudig subject (naam, `this`, ledenpad), op elke generieke `IEnumerable<T>` (lijst, array, dictionary).
- `Manual(reden)` voor gebruikt resultaat, ketens, `becauseArgs`, een subject met aanroep, en een assertion buiten een blok (lambda, `if` zonder accolades).

**Out of scope**

- `AllSatisfy` (eigen patroon `fa.all-satisfy`), `OnlyHaveUniqueItems`.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().OnlyContain(pred)` als statement in een blok staat THE SYSTEM SHALL het vervangen door `x.ShouldNotBeEmpty();` en `x.ShouldAllBe(pred);` (proof `OnlyContain/simple.*.cs`).
- **REQ-2**: WHEN het subject een `IEnumerable<T>`, array of dictionary is THE SYSTEM SHALL dezelfde herschrijving geven (proofs `enumerable.*`, `array.*`, `dictionary.*`).
- **REQ-3**: WHEN het subject een ledenpad is (`other.Items`, `this.Items`) THE SYSTEM SHALL het herschrijven (proof `member-subject.*`).
- **REQ-4**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als `customMessage` op beide statements zetten (proof `because.*`).
- **REQ-5**: IF `becauseArgs` of named arguments gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `because-args.*`).
- **REQ-6**: IF het resultaat wordt gebruikt (`.And`, variabele) THEN THE SYSTEM SHALL Manual geven (proofs `chain.*`, `used.*`).
- **REQ-7**: IF de assertion na `.And` staat THEN THE SYSTEM SHALL Manual geven (proof `and-chain.*`).
- **REQ-8**: IF het subject een aanroep is THEN THE SYSTEM SHALL Manual geven, omdat splitsen het subject twee keer evalueert (proof `subject-call.*`).
- **REQ-9**: IF de assertion geen statement direct in een blok is (lambda-body, `if` zonder accolades) THEN THE SYSTEM SHALL Manual geven, omdat er geen twee statements kunnen staan (proofs `lambda.*`, `no-block.*`).

## Assumptions

- Gedrag vastgelegd met een proefprogramma (FA 7.2.2, Shouldly 4.3.0): `OnlyContain` faalt op een lege collectie ("but the collection is empty"), `ShouldAllBe` slaagt; daarom de extra `ShouldNotBeEmpty()`.
- Het predicaat is bij FA altijd een `Expression<Func<T,bool>>`, net als bij Shouldly's `ShouldAllBe`; geen delegate-variant, geen conversie nodig.
- Getters in het subject zijn zuiver (twee keer evalueren is veilig).

## Risks

- Een `null`-collectie geeft bij FA een assertion-fout en bij Shouldly een `ArgumentNullException`/assertion-fout van `ShouldNotBeEmpty`: andere uitzondering, zelfde falen.
- Foutmelding verandert ("Additional Info:" i.p.v. FA's because-voorvoegsel).

## Modellen

Klassendiagram, sequence en beslisboom: [fa-only-contain-modellen.md](fa-only-contain-modellen.md).

## Open questions

- Geen.
