---
kingmadoc: 1
feature: "fa-be-same-as"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeSameAsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeSameAs/
---

# Feature: fa.be-same-as

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-093 (actie uit `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`x.Should().BeSameAs(y)` wordt `x.ShouldBeSameAs(y)`; `NotBeSameAs` wordt `ShouldNotBeSameAs`.

## Scope (in / out)

**In scope**

- `BeSameAs`/`NotBeSameAs` op elk FA-subject (ReferenceTypeAssertions, ook strings en collecties).
- `because` als customMessage; `.And`-schakel via `fa.and-chain`.

**Out of scope**

- becauseArgs, resultaat gebruikt (`.And`/`.Which`), AssertionScope: Manual.

## Requirements

- **REQ-1**: WHEN `BeSameAs(y)` staat THE SYSTEM SHALL `x.ShouldBeSameAs(y)` geven (proof `simple, nullable`).
- **REQ-2**: WHEN `NotBeSameAs(y)` staat THE SYSTEM SHALL `x.ShouldNotBeSameAs(y)` geven (proof `not`).
- **REQ-3**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage achteraan zetten (proof `because`).
- **REQ-4**: IF becauseArgs, een gebruikt resultaat of een AssertionScope THEN THE SYSTEM SHALL Manual geven (proof `because-args, result-used, assertion-scope`).
- **REQ-5**: WHEN `BeSameAs` een schakel na `.And` is THE SYSTEM SHALL via `fa.and-chain` een los statement geven (proof `AndChain/be-same-as`).

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0): beide gebruiken ReferenceEquals; gelijk oordeel bij zelfde, ander, beide null, één null en geboxte waarden.
- Shouldly 4.3.0 (reflectie): `ShouldBeSameAs(object?, object?, string?)` en `ShouldNotBeSameAs(object?, object?, string?)`.

## Risks

- Geen: identieke semantiek; alleen de foutmelding verschilt.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-same-as-modellen.md](fa-be-same-as-modellen.md).

## Zie ook

- [fa-be-same-as-verify.md](fa-be-same-as-verify.md)
- `claude/tasks/archief/TAAK-093-fa-be-same-as.md`
- [../README.md](../README.md)
