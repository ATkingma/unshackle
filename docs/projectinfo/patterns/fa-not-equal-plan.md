---
kingmadoc: 1
feature: "fa-not-equal"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/NotEqual/
---

# Feature: fa.not-equal: NotEqual(seq) wordt ShouldNotBe(seq), volgorde-gevoelig

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 met de hand (kingmadoc ontbreekt) |

> Opgesteld uit TAAK-160.

## One-sentence summary

`x.Should().NotEqual(y)` wordt `x.ShouldNotBe(y)`, met `ShouldNotBe<IEnumerable<T>>(y)` als de types verschillen.

## Scope (in / out)

**In scope**

- `NotEqual(IEnumerable<T>)` op `GenericCollectionAssertions` en `StringCollectionAssertions`, met optionele `because` en becauseArgs.

**Out of scope**

- Dictionary-`NotEqual` (volgorde-ongevoelig, Manual).
- `Equal` (zie `fa.equal-sequence`).

## Requirements

- **REQ-1**: WHEN `x.Should().NotEqual(y)` met gelijke types staat THE SYSTEM SHALL `x.ShouldNotBe(y)` geven (proofs `same-type`, `interface-type`, `string-collection`).
- **REQ-2**: WHEN subject en argument verschillend getypeerd zijn THE SYSTEM SHALL `x.ShouldNotBe<IEnumerable<T>>(y)` geven (proofs `different-type`, `array-literal`).
- **REQ-3**: WHEN `because` (met of zonder constante becauseArgs) staat THE SYSTEM SHALL `ShouldNotBe(y, because)` of met `string.Format(InvariantCulture, …)` geven (proofs `because`, `because-args`).
- **REQ-4**: WHEN de nullable-flow van het subject niet bewijst dat het niet-null is (maybe-null, of geen nullable-context) THE SYSTEM SHALL `x.ShouldNotBeNull().ShouldNotBe(y)` geven, omdat FA's `NotEqual` op null faalt en `ShouldNotBe` slaagt (proofs `nullable`, `same-type`); WHEN het bewezen niet-null is THE SYSTEM SHALL geen guard toevoegen (proof `non-null`).
- **REQ-5**: IF een dictionary, een AssertionScope of een gebruikt resultaat THEN THE SYSTEM SHALL Manual geven (proofs `dictionary`, `assertion-scope`, `result-used`).

## Assumptions

- Probe (Shouldly 4.3.0): `ShouldNotBe<IEnumerable<int>>` faalt op een gelijke reeks en slaagt bij andere volgorde of lengte.

## Risks

- Geen: het null-verschil (Shouldly slaagt op null, FA faalt) is afgedekt met `ShouldNotBeNull()` (REQ-4).

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-not-equal-modellen.md](fa-not-equal-modellen.md).

## Zie ook

- [fa-not-equal-verify.md](fa-not-equal-verify.md)
- `claude/tasks/archief/TAAK-160-fa-not-equal.md`
