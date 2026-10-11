---
kingmadoc: 1
feature: "fa-have-length"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/
---

# Feature: fa.have-length: string HaveLength(n) wordt Length.ShouldBe(n)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-039.

## One-sentence summary

`s.Should().HaveLength(n)` wordt `s.Length.ShouldBe(n)`; nullable subject `s.ShouldNotBeNull().Length.ShouldBe(n)`.

## Scope (in / out)

**In scope**

- `HaveLength(int)` op `StringAssertions`, met optionele `because`.

**Out of scope**

- Stream-`HaveLength(long)` (Manual).
- Subject met `?.` (Manual).

## Requirements

- **REQ-1**: WHEN `s.Should().HaveLength(n)` als statement staat THE SYSTEM SHALL `s.Length.ShouldBe(n)` geven (proof `simple`).
- **REQ-2**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage meegeven (proof `because`).
- **REQ-3**: WHEN het subject in een nullable context MaybeNull is THE SYSTEM SHALL `ShouldNotBeNull()` ervoor zetten (proof `nullable`).
- **REQ-4**: WHEN het subject een samengestelde expressie is THE SYSTEM SHALL het tussen haakjes zetten (proof `receiver-parens`).
- **REQ-5**: IF een stream, becauseArgs, een AssertionScope of `.And` THEN THE SYSTEM SHALL Manual geven (proofs `stream`, `because-args`, `assertion-scope`).
- **REQ-6**: WHEN `HaveLength` een schakel na `.And` is THE SYSTEM SHALL via `fa.and-chain` een los statement geven (proof `AndChain/equal-length-approximately`).
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (Fixed, proof `because-args`); benoemde argumenten of een niet-constante `because` blijven Manual. REQ-5 geldt daarmee niet meer voor becauseArgs.

## Assumptions

- Probe: null-string faalt bij FA en bij `ShouldNotBeNull()`; zonder nullable context geeft `s.Length` een NullReferenceException, de test faalt dus ook.

## Risks

- Melding bij null verschilt (NullReferenceException in plaats van assertion) als nullable uit staat.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-have-length-modellen.md](fa-have-length-modellen.md).
