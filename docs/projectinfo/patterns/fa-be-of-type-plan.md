---
kingmadoc: 1
feature: "fa-be-of-type"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/
---

# Feature: fa.be-of-type: BeOfType/BeAssignableTo becomes ShouldBeOfType/ShouldBeAssignableTo

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-034.

## One-sentence summary

fa.be-of-type: `BeOfType<T>()`, `BeAssignableTo<T>()`, `NotBeOfType<T>()`, `NotBeAssignableTo<T>()` (also with `Type`) become the Shouldly counterparts; `var v = ….Which` becomes `var v = x.ShouldBeOfType<T>()`.

## Scope (in / out)

**In scope**

- The four methods, generic or with a `Type` argument, with an optional `because`.
- `.Which`/`.Subject` as the only initializer of a declaration.

**Out of scope**

- `.Which.P.Should()...` chains (row `fa.which-chain`).
- `TypeAssertions` (`typeof(T).Should()`).

## Requirements

- **REQ-1**: WHEN `x.Should().BeOfType<T>()` (or one of the other three) is a statement THE SYSTEM SHALL give `x.ShouldBeOfType<T>()` etc. (proof `simple`).
- **REQ-2**: WHEN there is a `because` without becauseArgs THE SYSTEM SHALL pass it as customMessage (proof `because`).
- **REQ-3**: WHEN a `Type` argument is used THE SYSTEM SHALL give the non-generic Shouldly overload (proof `type-argument`).
- **REQ-4**: WHEN `var v = x.Should().BeOfType<T>().Which` (or `.Subject`) is used THE SYSTEM SHALL give `var v = x.ShouldBeOfType<T>()` (proof `which`).
- **REQ-5**: WHEN the subject is nullable THE SYSTEM SHALL rewrite without an extra null check (proof `nullable`).
- **REQ-6**: IF the result is used otherwise (`.And`, `.Which.P`, several declarators) THEN THE SYSTEM SHALL give Manual (proofs `and`, `which-chain`, `which-multiple`).
- **REQ-7**: IF becauseArgs, an AssertionScope or a `Type` subject THEN THE SYSTEM SHALL give Manual (proofs `because-args`, `assertion-scope`, `type-subject`).
- **Extension TAAK-096**: WHEN `.Which`/`.Subject` is used further (`….Which.Name`, several declarators) THE SYSTEM SHALL replace `.Which` with the Shouldly call, which returns the cast value (proofs `which-chain`, `which-multiple`); only `.And` and other usage stay Manual (`and`). Replaces that part of REQ-6.
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args`); named arguments or a non-constant `because` stay Manual. Replaces that part of REQ-7.

## Assumptions

- Shouldly 4.3.0 has `ShouldBeOfType<T>(this object? actual, string? customMessage)` and the `Type` overloads, which return the cast value.

## Risks

- The message text differs from FA; the behaviour (exact type vs. assignable) is the same.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-of-type-modellen.md](fa-be-of-type-modellen.md).
