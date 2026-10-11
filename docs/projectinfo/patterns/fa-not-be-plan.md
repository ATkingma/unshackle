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

# Feature: fa.not-be: x.Should().NotBe(y) wordt x.ShouldNotBe(y)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-032. Spiegel van `fa.should-be` (TAAK-001).

## One-sentence summary

fa.not-be: `x.Should().NotBe(y[, because])` wordt `x.ShouldNotBe(y[, because])`, met dezelfde regels als `fa.should-be`.

## Scope (in / out)

**In scope**

- Losse `NotBe(y)` met optionele `because`, ook op nullable subjects en records.
- Manual voor AssertionScope, keten, becauseArgs, comparer, Xml/Types en onveilige typeconversie.

**Out of scope**

- `.And.NotBe(...)` na een andere assertion (rij `fa.and-chain`).

## Requirements

- **REQ-1**: WHEN `x.Should().NotBe(y)` als statement staat THE SYSTEM SHALL `x.ShouldNotBe(y)` geven (proof `NotBe/simple.*`).
- **REQ-2**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als `customMessage` meegeven (proof `because.*`).
- **REQ-3**: WHEN het subject nullable is (`int?`, `string?`) THE SYSTEM SHALL `ShouldNotBe` geven zonder `ShouldNotBeNull()` (proof `nullable.*`).
- **REQ-4**: WHEN het subject een record is THE SYSTEM SHALL `ShouldNotBe` geven; beide gebruiken `Equals` (proof `record.*`).
- **REQ-5**: WHEN de constante alleen impliciet naar het subjecttype converteert THE SYSTEM SHALL een cast toevoegen (proof `constant-cast.*`).
- **REQ-6**: IF de assertion in een AssertionScope staat THEN THE SYSTEM SHALL Manual geven (proof `assertion-scope.*`).
- **REQ-7**: IF `becauseArgs` of een comparer worden gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `because-args.*`, `comparer.*`).
- **REQ-8**: IF het resultaat wordt gebruikt (`.And`) THEN THE SYSTEM SHALL Manual geven (proof `chain.*`).

## Assumptions

- Shouldly 4.3.0 heeft `ShouldNotBe<T>(this T actual, T expected, string? customMessage = null)`; T wordt uit beide kanten afgeleid, net als `ShouldBe`.

## Risks

- Null-subject: FA `NotBe("x")` slaagt op null; daarom bewust geen `NotNull`-helper (zou falen).
- Melding krijgt "Additional Info:" in plaats van FA's tekst: gedocumenteerd verschil.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-not-be-modellen.md](fa-not-be-modellen.md).
