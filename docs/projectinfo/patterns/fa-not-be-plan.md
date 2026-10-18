---
kingmadoc: 1
feature: "fa-not-be"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotBePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/NotBe/
---

# Feature: fa.not-be: x.Should().NotBe(y) becomes x.ShouldNotBe(y)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-032. Mirror of `fa.should-be` (TAAK-001).

## One-sentence summary

fa.not-be: `x.Should().NotBe(y[, because])` becomes `x.ShouldNotBe(y[, because])`, with the same rules as `fa.should-be`.

## Scope (in / out)

**In scope**

- Standalone `NotBe(y)` with optional `because`, also on nullable subjects and records.
- Manual for AssertionScope, chain, becauseArgs, comparer, Xml/Types and unsafe type conversion.

**Out of scope**

- `.And.NotBe(...)` after another assertion (row `fa.and-chain`).

## Requirements

- **REQ-1**: WHEN `x.Should().NotBe(y)` is a statement THE SYSTEM SHALL produce `x.ShouldNotBe(y)` (proof `NotBe/simple.*`).
- **REQ-2**: WHEN a `because` is present without `becauseArgs` THE SYSTEM SHALL pass it as `customMessage` (proof `because.*`).
- **REQ-3**: WHEN the subject is nullable (`int?`, `string?`) THE SYSTEM SHALL produce `ShouldNotBe` without `ShouldNotBeNull()` (proof `nullable.*`).
- **REQ-4**: WHEN the subject is a record THE SYSTEM SHALL produce `ShouldNotBe`; both use `Equals` (proof `record.*`).
- **REQ-5**: WHEN the constant only converts implicitly to the subject type THE SYSTEM SHALL add a cast (proof `constant-cast.*`).
- **REQ-6**: IF the assertion is inside an AssertionScope THEN THE SYSTEM SHALL give Manual (proof `assertion-scope.*`).
- **REQ-7**: IF `becauseArgs` or a comparer are used THEN THE SYSTEM SHALL give Manual (proofs `because-args.*`, `comparer.*`).
- **REQ-8**: IF the result is used (`.And`) THEN THE SYSTEM SHALL give Manual (proof `chain.*`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL pass `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (Fixed, proof `because-args`); named arguments or a non-constant `because` remain Manual. REQ-7 therefore only applies to the comparer.

- **Extension TAAK-277**: WHEN `NotBe<T>()`/`NotBe(typeof(T))` is called on TypeAssertions THE SYSTEM SHALL produce `ShouldNotBe(typeof(T))` (probed; proof `NotBe/type.*`).

## Assumptions

- Shouldly 4.3.0 has `ShouldNotBe<T>(this T actual, T expected, string? customMessage = null)`; T is inferred from both sides, just like `ShouldBe`.

## Risks

- Null subject: FA `NotBe("x")` passes on null; that is why there is deliberately no `NotNull` helper (it would fail).
- The message gets "Additional Info:" instead of FA's text: documented difference.

## Models

Class diagram, sequence diagram and decision tree: [fa-not-be-modellen.md](fa-not-be-modellen.md).
