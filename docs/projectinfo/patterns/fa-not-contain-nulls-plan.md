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

# Feature: fa.not-contain-nulls: NotContainNulls and AllBe become a predicate

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by hand (kingmadoc missing) |

> Drawn up from TAAK-161.

## One-sentence summary

`x.Should().NotContainNulls()` becomes `x.ShouldNotContain(i => i == null)` and `x.Should().AllBe("v")` becomes `x.ShouldAllBe(i => i == "v")`.

## Scope (in / out)

**In scope**

- `NotContainNulls()` on generic collections, with `because` and becauseArgs.
- `AllBe(string)` on `StringCollectionAssertions` (in FluentAssertions 7.2.2 the only type with `AllBe`).

**Out of scope**

- `NotContainNulls(selector)`: Manual.
- `AllBe` on other than string collections does not exist in FA 7.2.2.

## Requirements

- **REQ-1**: WHEN `x.Should().NotContainNulls()` is used THE SYSTEM SHALL give `x.ShouldNotContain(i => i == null)` (proofs `simple`, `array`, `nullable-int`).
- **REQ-2**: WHEN `x.Should().AllBe("v")` is on a string collection THE SYSTEM SHALL give `x.ShouldAllBe(i => i == "v")` (proofs `all-be-string`, `all-be-member-expected`, `all-be-constant-expression`).
- **REQ-3**: WHEN there is a `because` (with or without constant becauseArgs) THE SYSTEM SHALL pass it on (proofs `because`, `because-args`, `all-be-because`, `all-be-because-args`).
- **REQ-4**: WHEN the parameter name `i` is already in scope THE SYSTEM SHALL choose a free name (proofs `name-conflict`, `all-be-name-conflict`).
- **REQ-5**: WHEN the subject is nullable THE SYSTEM SHALL put `ShouldNotBeNull()` before the call (proofs `nullable`, `all-be-nullable-string`).
- **REQ-6**: IF a selector, a non-nullable value type, a computed expected value, an AssertionScope or a used result THEN THE SYSTEM SHALL give Manual (proofs `selector`, `value-type`, `all-be-computed`, `assertion-scope`, `all-be-assertion-scope`, `result-used`, `all-be-result-used`).

## Assumptions

- On an empty collection both sides pass.
- `AllBe` on strings compares ordinally, equal to `==`.

## Risks

- A computed expected value would be evaluated per element in the expression; hence Manual.

## Models

Class diagram, sequence diagram and decision tree: [fa-not-contain-nulls-modellen.md](fa-not-contain-nulls-modellen.md).

## See also

- [fa-not-contain-nulls-verify.md](fa-not-contain-nulls-verify.md)
- `claude/tasks/archief/TAAK-161-fa-not-contain-nulls.md`
