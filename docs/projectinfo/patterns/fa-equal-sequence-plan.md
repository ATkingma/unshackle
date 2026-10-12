---
kingmadoc: 1
feature: "fa-equal-sequence"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/
---

# Feature: fa.equal-sequence: Equal(seq) en Equal(params) worden ShouldBe(seq), volgorde-gevoelig

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-038.

## One-sentence summary

`x.Should().Equal(y)` wordt `x.ShouldBe(y)`; `Equal(1, 2)` wordt `x.ShouldBe(new T[] { 1, 2 })`; met `because` `x.ShouldBe(y, ignoreOrder: false, because)`.

## Scope (in / out)

**In scope**

- `Equal(IEnumerable<T>)`, `Equal(params T[])` op `GenericCollectionAssertions` en `StringCollectionAssertions`, met optionele `because`.

**Out of scope**

- `NotEqual` (sinds TAAK-160 eigen patroon `fa.not-equal`).
- Dictionary-`Equal` (volgorde-ongevoelig, Manual).
- `Equal(seq, predicate)` (Manual).

## Requirements

- **REQ-1**: WHEN `x.Should().Equal(y)` als statement staat THE SYSTEM SHALL `x.ShouldBe(y)` geven (proofs `simple`, `params-array`).
- **REQ-2**: WHEN `Equal(a, b, ...)` met losse params staat THE SYSTEM SHALL `x.ShouldBe(new T[] { a, b })` geven met T het element-type van FA (proof `params`).
- **REQ-3**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL `ShouldBe(y, ignoreOrder: false, because)` geven (proof `because`).
- **REQ-4**: WHEN het subject nullable is THE SYSTEM SHALL zonder null-check herschrijven; Shouldly faalt zelf op null (proof `nullable`).
- **REQ-5**: IF een dictionary, een predicate, becauseArgs, een AssertionScope of `.And` THEN THE SYSTEM SHALL Manual geven (proofs `dictionary`, `predicate`, `because-args`, `assertion-scope`, `and`).
- **REQ-6**: WHEN `Equal` een schakel na `.And` is THE SYSTEM SHALL via `fa.and-chain` een los `ShouldBe`-statement geven (proof `AndChain/equal-length-approximately`).
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `ShouldBe(y, ignoreOrder: false, string.Format(CultureInfo.InvariantCulture, because, args))` geven (Fixed, proof `because-args`); benoemde argumenten of een niet-constante `because` blijven Manual. REQ-5 geldt daarmee niet meer voor becauseArgs.

## Assumptions

- Semantiek gecontroleerd met een probe (FA 7.2.2 + Shouldly 4.3.0): beide volgorde-gevoelig, element-voor-element, ongelijke lengte faalt, null-subject faalt bij beide.
- Shouldly 4.3.0: `ShouldBe<T>(IEnumerable<T>, IEnumerable<T>, bool ignoreOrder = false)` en `(…, bool ignoreOrder, string? customMessage)`; geen `ShouldNotBe` voor sequences.

## Risks

- Strings in de collectie: FA en Shouldly vergelijken allebei hoofdlettergevoelig (probe `strs`).

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-equal-sequence-modellen.md](fa-equal-sequence-modellen.md).
