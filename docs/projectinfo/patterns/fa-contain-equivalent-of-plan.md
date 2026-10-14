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

# Feature: fa.contain-equivalent-of: ContainEquivalentOf becomes ShouldContain for value items

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by hand (kingmadoc unavailable) |

> Drafted from TAAK-164.

## One-sentence summary

`x.Should().ContainEquivalentOf(item)` becomes `x.ShouldContain(item)` (and `NotContainEquivalentOf` becomes `ShouldNotContain`) if `item` has the element type and that type is a value item; otherwise Manual.

## Scope (in / out)

**In scope**

- `ContainEquivalentOf(item)` and `NotContainEquivalentOf(item)` on generic collections, with `because` and becauseArgs.
- Value items: leaf types (including nullable), sealed records, record structs and structs with leaf members.

**Out of scope**

- The overload with `config` (equivalency options), classes without value equality, an expectation type different from the element type, dictionaries: Manual.

## Requirements

- **REQ-1**: WHEN `x.Should().ContainEquivalentOf(item)` or `NotContainEquivalentOf(item)` is used on a collection of leaf items THE SYSTEM SHALL produce `ShouldContain(item)` or `ShouldNotContain(item)` respectively (proofs `simple`, `strings`, `nullable-value`).
- **REQ-2**: WHEN the element is a sealed record or record struct THE SYSTEM SHALL produce the same rewrite (proofs `sealed-record`, `record-struct`).
- **REQ-3**: WHEN `because` (with or without constant becauseArgs) is present THE SYSTEM SHALL pass it on (proof `because`).
- **REQ-4**: WHEN the subject is nullable THE SYSTEM SHALL put `ShouldNotBeNull()` before the call (proof `nullable-subject`).
- **REQ-5**: IF the element is a class, a non-sealed record, a record with a collection member or a `KeyValuePair`, or the expectation type differs from the element type THEN THE SYSTEM SHALL produce Manual with a specific reason (proofs `class`, `record-unsealed`, `record-list-member`, `dictionary`, `different-type`).
- **REQ-6**: IF an options lambda, an AssertionScope or a used result (`.Which`) THEN THE SYSTEM SHALL produce Manual (proofs `options`, `assertion-scope`, `which`).

## Assumptions

- FA compares strings in equivalency case-sensitively and ordinally (tried), same as `ShouldContain` on a collection.
- On a null subject both sides fail (tried).

## Risks

- A class with its own `Equals` would be equal, but `IsValueLike` deliberately rejects it (Manual).

## Models

Class diagram, sequence diagram and decision tree: [fa-contain-equivalent-of-modellen.md](fa-contain-equivalent-of-modellen.md).

## See also

- [fa-contain-equivalent-of-verify.md](fa-contain-equivalent-of-verify.md)
- `claude/tasks/archief/TAAK-164-fa-contain-equivalent-of.md`
