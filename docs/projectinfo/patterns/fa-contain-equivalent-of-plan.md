---
kingmadoc: 1
feature: "fa-contain-equivalent-of"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/
---

# Feature: fa.contain-equivalent-of: ContainEquivalentOf wordt ShouldContain voor waarde-items

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 met de hand (kingmadoc ontbreekt) |

> Opgesteld uit TAAK-164.

## One-sentence summary

`x.Should().ContainEquivalentOf(item)` wordt `x.ShouldContain(item)` (en `NotContainEquivalentOf` wordt `ShouldNotContain`) als `item` het elementtype heeft en dat type een waarde-item is; anders Manual.

## Scope (in / out)

**In scope**

- `ContainEquivalentOf(item)` en `NotContainEquivalentOf(item)` op generieke collecties, met `because` en becauseArgs.
- Waarde-items: leaf-types (ook nullable), sealed records, record structs en structs met leaf-leden.

**Out of scope**

- De overload met `config` (equivalency options), classes zonder waarde-gelijkheid, ander verwachtingstype dan elementtype, dictionaries: Manual.

## Requirements

- **REQ-1**: WHEN `x.Should().ContainEquivalentOf(item)` of `NotContainEquivalentOf(item)` op een collectie van leaf-items staat THE SYSTEM SHALL `ShouldContain(item)` resp. `ShouldNotContain(item)` geven (proofs `simple`, `strings`, `nullable-value`).
- **REQ-2**: WHEN het element een sealed record of record struct is THE SYSTEM SHALL dezelfde herschrijving geven (proofs `sealed-record`, `record-struct`).
- **REQ-3**: WHEN `because` (met of zonder constante becauseArgs) staat THE SYSTEM SHALL die doorgeven (proof `because`).
- **REQ-4**: WHEN het subject nullable is THE SYSTEM SHALL `ShouldNotBeNull()` voor de aanroep zetten (proof `nullable-subject`).
- **REQ-5**: IF het element een class, een niet-sealed record, een record met collectie-lid of een `KeyValuePair` is, of het verwachtingstype verschilt van het elementtype THEN THE SYSTEM SHALL Manual geven met een specifieke reden (proofs `class`, `record-unsealed`, `record-list-member`, `dictionary`, `different-type`).
- **REQ-6**: IF een options-lambda, een AssertionScope of een gebruikt resultaat (`.Which`) THEN THE SYSTEM SHALL Manual geven (proofs `options`, `assertion-scope`, `which`).

## Assumptions

- FA vergelijkt strings in equivalency hoofdlettergevoelig en ordinaal (geprobeerd), gelijk aan `ShouldContain` op een collectie.
- Op een null-subject falen beide kanten (geprobeerd).

## Risks

- Een class met eigen `Equals` zou gelijk zijn, maar `IsValueLike` weigert die bewust (Manual).

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-contain-equivalent-of-modellen.md](fa-contain-equivalent-of-modellen.md).

## Zie ook

- [fa-contain-equivalent-of-verify.md](fa-contain-equivalent-of-verify.md)
- `claude/tasks/archief/TAAK-164-fa-contain-equivalent-of.md`
