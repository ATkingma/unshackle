---
kingmadoc: 1
feature: "fa-direct-mapping"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DirectMappingPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/DirectMapping/
---

# Feature: fa.direct-mapping

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-101 (actie uit `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`BeOneOf` → `ShouldBeOneOf`, `BeSubsetOf` → `ShouldBeSubsetOf`, `OnlyHaveUniqueItems` → `ShouldBeUnique`, `HaveValue`/`NotHaveValue` → `ShouldNotBeNull`/`ShouldBeNull`; `ContainInOrder` → Manual met code.

## Scope (in / out)

**In scope**: de vormen uit de requirements; `because` (ook met becauseArgs, centraal) als customMessage; `.And`-schakel via `fa.and-chain`.

**Out of scope**: alles wat de requirements als Manual noemen.

## Requirements

- **REQ-1**: WHEN `BeOneOf(a, b)` (params) staat THE SYSTEM SHALL `x.ShouldBeOneOf(a, b)` geven, met cast zoals `ShouldBe` (proof `be-one-of, be-one-of-convert`).
- **REQ-2**: WHEN `BeSubsetOf(y)` / `OnlyHaveUniqueItems()` staat THE SYSTEM SHALL `ShouldBeSubsetOf(y)` / `ShouldBeUnique()` geven (proof `be-subset-of, unique`).
- **REQ-3**: WHEN `HaveValue()`/`NotHaveValue()` op `Nullable<T>` staat THE SYSTEM SHALL `ShouldNotBeNull()`/`ShouldBeNull()` geven (proof `have-value, not-have-value`).
- **REQ-4**: IF `BeOneOf(IEnumerable)`, `OnlyHaveUniqueItems(keySelector)` of `ContainInOrder` THEN THE SYSTEM SHALL Manual geven (ContainInOrder met concrete code in de reden) (proof `be-one-of-enumerable, unique-key, contain-in-order`).

## Assumptions

- Probe: BeOneOf (null, record, class, hoofdletters), BeSubsetOf (dubbelen, leeg, null, record vs class), OnlyHaveUniqueItems (nulls, leeg, record vs class), HaveValue/NotHaveValue: gelijk oordeel.
- Reflectie Shouldly 4.3.0: `ShouldBeOneOf<T>(T, params T[])` (customMessage alleen ná een array), `ShouldBeSubsetOf<T>(IEnumerable<T>, IEnumerable<T>, string?)`, `ShouldBeUnique<T>(IEnumerable<T>, string?)`, `ShouldNotBeNull<T>(T?, string?)`, `ShouldBeNull<T>(T?, string?)`.
- Shouldly heeft geen `ShouldContainInOrder`; een shim is bewust niet gebouwd (zie ANALYSE-fa-100).

## Risks

- Alleen de foutmelding verschilt; waar het oordeel kon verschillen is het Manual.

## Modellen

Klassen, volgorde en beslisboom: [fa-direct-mapping-modellen.md](fa-direct-mapping-modellen.md).

## Zie ook

- [fa-direct-mapping-verify.md](fa-direct-mapping-verify.md)
- `claude/tasks/archief/TAAK-101-fa-direct-mappings.md`
- [../README.md](../README.md)
