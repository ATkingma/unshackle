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

# Feature: fa.string-start-end: StartWith/EndWith en NullOrEmpty worden ShouldStartWith/ShouldEndWith en ShouldBeNullOrEmpty

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-035.

## One-sentence summary

`s.Should().StartWith(x)` en familie worden `s.ShouldStartWith(x, Shouldly.Case.Sensitive)`; `NotBeNullOrEmpty()` e.d. worden `ShouldNotBeNullOrEmpty()` e.d.

## Scope (in / out)

**In scope**

- `StartWith`, `EndWith`, `NotStartWith`, `NotEndWith` en hun `EquivalentOf`-varianten op `StringAssertions`, met optionele `because`.
- `BeNullOrEmpty`, `NotBeNullOrEmpty`, `BeNullOrWhiteSpace`, `NotBeNullOrWhiteSpace`.

**Out of scope**

- Collectie-`StartWith`/`EndWith` en collectie-`NotBeNullOrEmpty` (Manual).
- `EquivalentOf` met equivalency-options (Manual).

## Requirements

- **REQ-1**: WHEN `StartWith`/`EndWith`/`NotStartWith`/`NotEndWith` als statement staat THE SYSTEM SHALL de Shouldly-methode met `Shouldly.Case.Sensitive` geven (proof `simple`).
- **REQ-2**: WHEN een `EquivalentOf`-variant staat THE SYSTEM SHALL de Shouldly-methode zonder `Case` (default `Insensitive`) geven (proof `equivalent-of`).
- **REQ-3**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage op de juiste plek zetten, ook bij de afwijkende `ShouldNotEndWith`-volgorde (proof `because`).
- **REQ-4**: WHEN `(Not)BeNullOr(Empty|WhiteSpace)` staat THE SYSTEM SHALL `Should(Not)BeNullOr…` geven (proof `null-or`).
- **REQ-5**: WHEN het subject nullable of een member/interpolatie is THE SYSTEM SHALL zonder extra null-check herschrijven (proofs `nullable`, `edge-member-subject`).
- **REQ-6**: IF becauseArgs, een AssertionScope, `.And` of een niet-string subject THEN THE SYSTEM SHALL Manual geven (proofs `because-args`, `assertion-scope`, `and`, `collection`).
- **Uitbreiding TAAK-097**: WHEN becauseArgs bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage zetten (proof `because-args`, nu `.after.cs`); named arguments of een niet-constante `because` blijven Manual. Vervangt het becauseArgs-deel van REQ-6.

## Assumptions

- Shouldly 4.3.0 (gecontroleerd met reflectie): `ShouldStartWith/EndWith/NotStartWith(string? actual, string expected, Case caseSensitivity = Insensitive, string? customMessage)`; `ShouldNotEndWith` heeft `(expected, Case)` en `(expected, customMessage, Case = Insensitive)`; `Should(Not)BeNullOr(Empty|WhiteSpace)(string?, customMessage)`.

## Risks

- Shouldly's default is case-insensitive; vergeten `Case.Sensitive` zou tests stil zwakker maken. Afgedekt door proof `simple`.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-string-start-end-modellen.md](fa-string-start-end-modellen.md).
