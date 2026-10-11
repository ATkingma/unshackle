---
kingmadoc: 1
feature: "fa-contain-single-which"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/
---

# Feature: fa.contain-single-which: ContainSingle wordt ShouldHaveSingleItem of ShouldContain(pred, 1)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-036.

## One-sentence summary

`x.Should().ContainSingle()` wordt `x.ShouldHaveSingleItem()`, `ContainSingle(pred)` wordt `x.ShouldContain(pred, 1)` en `var y = ….ContainSingle(pred).Which` wordt `var y = x.Where(pred).ShouldHaveSingleItem()`.

## Scope (in / out)

**In scope**

- `ContainSingle()` en `ContainSingle(predicate)` met optionele `because`.
- `.Which` als enige initializer van een declaratie.

**Out of scope**

- `.Which.P.Should()...`-ketens (rij `fa.which-chain`).
- Niet-generieke collecties (Manual).

## Requirements

- **REQ-1**: WHEN `x.Should().ContainSingle()` als statement staat THE SYSTEM SHALL `x.ShouldHaveSingleItem()` geven (proof `simple`).
- **REQ-2**: WHEN `ContainSingle(pred)` als statement staat THE SYSTEM SHALL `x.ShouldContain(pred, 1)` geven (proofs `predicate`, `enumerable`).
- **REQ-3**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage meegeven (proof `because`).
- **REQ-4**: WHEN `var y = x.Should().ContainSingle([lambda]).Which` staat THE SYSTEM SHALL `var y = x[.Where(lambda)].ShouldHaveSingleItem()` geven, met `using System.Linq` (proof `which`).
- **REQ-5**: WHEN het subject nullable is THE SYSTEM SHALL `ShouldNotBeNull()` ervoor zetten als de flow-state MaybeNull is (proof `nullable`).
- **REQ-6**: IF het resultaat verder wordt gebruikt, de predicate een Expression-variabele bij `.Which` is, becauseArgs of een AssertionScope THEN THE SYSTEM SHALL Manual geven (proofs `which-chain`, `expression-variable`, `because-args`, `assertion-scope`).
- **Uitbreiding TAAK-096**: WHEN `.Which`/`.Subject` verder gebruikt wordt (`….Which.Price`) THE SYSTEM SHALL `.Which` vervangen door de Shouldly-aanroep, die het item teruggeeft (proof `which-chain`); `.And` en ander gebruik blijven Manual. Vervangt dat deel van REQ-6.
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (proof `because-args`); benoemde argumenten of een niet-constante `because` blijven Manual. Vervangt dat deel van REQ-6.

## Assumptions

- Shouldly 4.3.0 (reflectie): `ShouldHaveSingleItem<T>(IEnumerable<T>, customMessage)` geeft T terug; `ShouldContain<T>(IEnumerable<T>, Expression<Func<T,bool>>, int expectedCount, customMessage)`.

## Risks

- Meldingstekst verschilt van FA; gedrag (precies één (match)) is gelijk.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-contain-single-which-modellen.md](fa-contain-single-which-modellen.md).
