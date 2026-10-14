---
kingmadoc: 1
feature: "fa-string-start-end"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/StringStartEndPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/StringStartEnd/
---

# Feature: fa.string-start-end: StartWith/EndWith and NullOrEmpty become ShouldStartWith/ShouldEndWith and ShouldBeNullOrEmpty

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-035.

## One-sentence summary

`s.Should().StartWith(x)` and family become `s.ShouldStartWith(x, Shouldly.Case.Sensitive)`; `NotBeNullOrEmpty()` etc. become `ShouldNotBeNullOrEmpty()` etc.

## Scope (in / out)

**In scope**

- `StartWith`, `EndWith`, `NotStartWith`, `NotEndWith` and their `EquivalentOf` variants on `StringAssertions`, with an optional `because`.
- `BeNullOrEmpty`, `NotBeNullOrEmpty`, `BeNullOrWhiteSpace`, `NotBeNullOrWhiteSpace`.

**Out of scope**

- Collection `StartWith`/`EndWith` and collection `NotBeNullOrEmpty` (Manual).
- `EquivalentOf` with equivalency options (Manual).

## Requirements

- **REQ-1**: WHEN `StartWith`/`EndWith`/`NotStartWith`/`NotEndWith` is a statement THE SYSTEM SHALL give the Shouldly method with `Shouldly.Case.Sensitive` (proof `simple`).
- **REQ-2**: WHEN an `EquivalentOf` variant is used THE SYSTEM SHALL give the Shouldly method without `Case` (default `Insensitive`) (proof `equivalent-of`).
- **REQ-3**: WHEN there is a `because` without becauseArgs THE SYSTEM SHALL put it as customMessage in the right place, also with the deviating `ShouldNotEndWith` order (proof `because`).
- **REQ-4**: WHEN `(Not)BeNullOr(Empty|WhiteSpace)` is used THE SYSTEM SHALL give `Should(Not)BeNullOr…` (proof `null-or`).
- **REQ-5**: WHEN the subject is nullable or a member/interpolation THE SYSTEM SHALL rewrite without an extra null check (proofs `nullable`, `edge-member-subject`).
- **REQ-6**: IF becauseArgs, an AssertionScope, `.And` or a non-string subject THEN THE SYSTEM SHALL give Manual (proofs `because-args`, `assertion-scope`, `and`, `collection`).
- **Extension TAAK-097**: WHEN becauseArgs accompany a constant, formattable `because` THE SYSTEM SHALL put `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args`, now `.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces the becauseArgs part of REQ-6.

## Assumptions

- Shouldly 4.3.0 (checked with reflection): `ShouldStartWith/EndWith/NotStartWith(string? actual, string expected, Case caseSensitivity = Insensitive, string? customMessage)`; `ShouldNotEndWith` has `(expected, Case)` and `(expected, customMessage, Case = Insensitive)`; `Should(Not)BeNullOr(Empty|WhiteSpace)(string?, customMessage)`.

## Risks

- Shouldly's default is case-insensitive; a forgotten `Case.Sensitive` would silently make tests weaker. Covered by proof `simple`.

## Models

Class diagram, sequence diagram and decision tree: [fa-string-start-end-modellen.md](fa-string-start-end-modellen.md).
