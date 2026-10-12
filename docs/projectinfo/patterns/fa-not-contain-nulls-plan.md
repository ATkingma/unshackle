---
kingmadoc: 1
feature: "fa-not-contain-nulls"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotContainNullsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/NotContainNulls/
---

# Feature: fa.not-contain-nulls: NotContainNulls en AllBe worden een predicaat

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 met de hand (kingmadoc ontbreekt) |

> Opgesteld uit TAAK-161.

## One-sentence summary

`x.Should().NotContainNulls()` wordt `x.ShouldNotContain(i => i == null)` en `x.Should().AllBe("v")` wordt `x.ShouldAllBe(i => i == "v")`.

## Scope (in / out)

**In scope**

- `NotContainNulls()` op generieke collecties, met `because` en becauseArgs.
- `AllBe(string)` op `StringCollectionAssertions` (in FluentAssertions 7.2.2 het enige type met `AllBe`).

**Out of scope**

- `NotContainNulls(selector)`: Manual.
- `AllBe` op andere dan string-verzamelingen bestaat niet in FA 7.2.2.

## Requirements

- **REQ-1**: WHEN `x.Should().NotContainNulls()` staat THE SYSTEM SHALL `x.ShouldNotContain(i => i == null)` geven (proofs `simple`, `array`, `nullable-int`).
- **REQ-2**: WHEN `x.Should().AllBe("v")` op een string-verzameling staat THE SYSTEM SHALL `x.ShouldAllBe(i => i == "v")` geven (proofs `all-be-string`, `all-be-member-expected`, `all-be-constant-expression`).
- **REQ-3**: WHEN `because` (met of zonder constante becauseArgs) staat THE SYSTEM SHALL die doorgeven (proofs `because`, `because-args`, `all-be-because`, `all-be-because-args`).
- **REQ-4**: WHEN de parameternaam `i` al in scope is THE SYSTEM SHALL een vrije naam kiezen (proofs `name-conflict`, `all-be-name-conflict`).
- **REQ-5**: WHEN het subject nullable is THE SYSTEM SHALL `ShouldNotBeNull()` voor de aanroep zetten (proofs `nullable`, `all-be-nullable-string`).
- **REQ-6**: IF een selector, een niet-nullable waardetype, een berekende verwachte waarde, een AssertionScope of een gebruikt resultaat THEN THE SYSTEM SHALL Manual geven (proofs `selector`, `value-type`, `all-be-computed`, `assertion-scope`, `all-be-assertion-scope`, `result-used`, `all-be-result-used`).

## Assumptions

- Op een lege collectie slagen beide kanten.
- `AllBe` op strings vergelijkt ordinaal, gelijk aan `==`.

## Risks

- Een berekende verwachte waarde zou in de expressie per element worden geëvalueerd; daarom Manual.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-not-contain-nulls-modellen.md](fa-not-contain-nulls-modellen.md).

## Zie ook

- [fa-not-contain-nulls-verify.md](fa-not-contain-nulls-verify.md)
- `claude/tasks/archief/TAAK-161-fa-not-contain-nulls.md`
