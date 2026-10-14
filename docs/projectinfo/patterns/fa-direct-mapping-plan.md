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

> Drawn up from TAAK-101 (action from `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`BeOneOf` → `ShouldBeOneOf`, `BeSubsetOf` → `ShouldBeSubsetOf`, `OnlyHaveUniqueItems` → `ShouldBeUnique`, `HaveValue`/`NotHaveValue` → `ShouldNotBeNull`/`ShouldBeNull`; `ContainInOrder` → Manual with code.

## Scope (in / out)

**In scope**: the forms from the requirements; `because` (also with becauseArgs, centrally) as customMessage; an `.And` link via `fa.and-chain`.

**Out of scope**: everything the requirements name as Manual.

## Requirements

- **REQ-1**: WHEN `BeOneOf(a, b)` (params) is used THE SYSTEM SHALL give `x.ShouldBeOneOf(a, b)`, with a cast like `ShouldBe` (proof `be-one-of, be-one-of-convert`).
- **REQ-2**: WHEN `BeSubsetOf(y)` / `OnlyHaveUniqueItems()` is used THE SYSTEM SHALL give `ShouldBeSubsetOf(y)` / `ShouldBeUnique()` (proof `be-subset-of, unique`).
- **REQ-3**: WHEN `HaveValue()`/`NotHaveValue()` is on `Nullable<T>` THE SYSTEM SHALL give `ShouldNotBeNull()`/`ShouldBeNull()` (proof `have-value, not-have-value`).
- **REQ-4**: IF `BeOneOf(IEnumerable)`, `OnlyHaveUniqueItems(keySelector)` or `ContainInOrder` THEN THE SYSTEM SHALL give Manual (ContainInOrder with concrete code in the reason) (proof `be-one-of-enumerable, unique-key, contain-in-order`).

## Assumptions

- Probe: BeOneOf (null, record, class, case), BeSubsetOf (duplicates, empty, null, record vs class), OnlyHaveUniqueItems (nulls, empty, record vs class), HaveValue/NotHaveValue: equal verdict.
- Reflection Shouldly 4.3.0: `ShouldBeOneOf<T>(T, params T[])` (customMessage only after an array), `ShouldBeSubsetOf<T>(IEnumerable<T>, IEnumerable<T>, string?)`, `ShouldBeUnique<T>(IEnumerable<T>, string?)`, `ShouldNotBeNull<T>(T?, string?)`, `ShouldBeNull<T>(T?, string?)`.
- Shouldly has no `ShouldContainInOrder`; a shim was deliberately not built (see ANALYSE-fa-100).

## Risks

- Only the error message differs; where the verdict could differ it is Manual.

## Models

Classes, sequence and decision tree: [fa-direct-mapping-modellen.md](fa-direct-mapping-modellen.md).

## See also

- [fa-direct-mapping-verify.md](fa-direct-mapping-verify.md)
- `claude/tasks/archief/TAAK-101-fa-direct-mappings.md`
- [../README.md](../README.md)
