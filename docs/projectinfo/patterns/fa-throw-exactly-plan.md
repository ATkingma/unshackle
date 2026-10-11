---
kingmadoc: 1
feature: "fa-throw-exactly"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/ThrowExactly/
---

# Feature: fa.throw-exactly

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-051.

## One-sentence summary

`act.Should().ThrowExactly<T>()` wordt `Should.Throw<T>(act).ShouldBeOfType<T>()`; `await act.Should().ThrowExactlyAsync<T>()` wordt `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()`; `.WithMessage` zoals `fa.throw`.

## Scope (in / out)

**In scope**

- `ThrowExactly<T>` en `ThrowExactlyAsync<T>` als statement, met optionele `because` en `.WithMessage(p)`.

**Out of scope**

- Invoking/Awaiting-subject (hoort bij `fa.invoking`, dat dezelfde rewrite gebruikt).
- Gebruikt resultaat (`.Which`, `.Where`, `.And`): Manual.

## Requirements

- **REQ-1**: WHEN `act.Should().ThrowExactly<T>()` als statement staat THE SYSTEM SHALL `Should.Throw<T>(act).ShouldBeOfType<T>()` geven (proof `simple`).
- **REQ-2**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage aan `Should.Throw` meegeven (proof `because`).
- **REQ-3**: WHEN `.WithMessage(p)` volgt THE SYSTEM SHALL de message-check van `fa.throw` op het `ShouldBeOfType`-resultaat zetten (proof `message, async-message`).
- **REQ-4**: WHEN `await act.Should().ThrowExactlyAsync<T>()` staat THE SYSTEM SHALL `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()` geven (proof `async`).
- **REQ-5**: WHEN de actie `Wait()`/`.Result` gebruikt THE SYSTEM SHALL toch herschrijven: ThrowExactly pakt AggregateException niet uit, Shouldly ook niet (proof `aggregate-wait`).
- **REQ-6**: IF becauseArgs, een AssertionScope of een gebruikt resultaat THEN THE SYSTEM SHALL Manual geven (proof `because-args, assertion-scope, value-used`).

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0, scratchpad `probe/`): exact type, afgeleid type, basistype, geen exceptie en AggregateException geven bij beide dezelfde uitkomst (pass/fail), sync en async.

## Risks

- Bij een afgeleid type faalt Shouldly in `ShouldBeOfType` in plaats van in `Throw`; de melding verschilt, de uitkomst niet. De `because` staat alleen op `Should.Throw`.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-throw-exactly-modellen.md](fa-throw-exactly-modellen.md).
