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

# Feature: fa.not-equal: NotEqual(seq) becomes ShouldNotBe(seq), order-sensitive

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by hand (kingmadoc missing) |

> Drawn up from TAAK-160.

## One-sentence summary

`x.Should().NotEqual(y)` becomes `x.ShouldNotBe(y)`, with `ShouldNotBe<IEnumerable<T>>(y)` when the types differ.

## Scope (in / out)

**In scope**

- `NotEqual(IEnumerable<T>)` on `GenericCollectionAssertions` and `StringCollectionAssertions`, with an optional `because` and becauseArgs.

**Out of scope**

- Dictionary `NotEqual` (order-insensitive, Manual).
- `Equal` (see `fa.equal-sequence`).

## Requirements

- **REQ-1**: WHEN `x.Should().NotEqual(y)` with equal types is used THE SYSTEM SHALL give `x.ShouldNotBe(y)` (proofs `same-type`, `interface-type`, `string-collection`).
- **REQ-2**: WHEN subject and argument are typed differently THE SYSTEM SHALL give `x.ShouldNotBe<IEnumerable<T>>(y)` (proofs `different-type`, `array-literal`).
- **REQ-3**: WHEN there is a `because` (with or without constant becauseArgs) THE SYSTEM SHALL give `ShouldNotBe(y, because)` or with `string.Format(InvariantCulture, …)` (proofs `because`, `because-args`).
- **REQ-4**: WHEN the nullable flow of the subject does not prove it is non-null (maybe-null, or no nullable context) THE SYSTEM SHALL give `x.ShouldNotBeNull().ShouldNotBe(y)`, because FA's `NotEqual` fails on null and `ShouldNotBe` passes (proofs `nullable`, `same-type`); WHEN it is proven non-null THE SYSTEM SHALL add no guard (proof `non-null`).
- **REQ-5**: IF a dictionary, an AssertionScope or a used result THEN THE SYSTEM SHALL give Manual (proofs `dictionary`, `assertion-scope`, `result-used`).

## Assumptions

- Probe (Shouldly 4.3.0): `ShouldNotBe<IEnumerable<int>>` fails on an equal sequence and passes with another order or length.

## Risks

- None: the null difference (Shouldly passes on null, FA fails) is covered with `ShouldNotBeNull()` (REQ-4).

## Models

Class diagram, sequence diagram and decision tree: [fa-not-equal-modellen.md](fa-not-equal-modellen.md).

## See also

- [fa-not-equal-verify.md](fa-not-equal-verify.md)
- `claude/tasks/archief/TAAK-160-fa-not-equal.md`
