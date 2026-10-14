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

> Drawn up from TAAK-051.

## One-sentence summary

`act.Should().ThrowExactly<T>()` becomes `Should.Throw<T>(act).ShouldBeOfType<T>()`; `await act.Should().ThrowExactlyAsync<T>()` becomes `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()`; `.WithMessage` as in `fa.throw`.

## Scope (in / out)

**In scope**

- `ThrowExactly<T>` and `ThrowExactlyAsync<T>` as a statement, with optional `because` and `.WithMessage(p)`.

**Out of scope**

- Invoking/Awaiting subject (belongs to `fa.invoking`, which uses the same rewrite).
- Used result (`.Which`, `.Where`, `.And`): Manual.

## Requirements

- **REQ-1**: WHEN `act.Should().ThrowExactly<T>()` is a statement THE SYSTEM SHALL produce `Should.Throw<T>(act).ShouldBeOfType<T>()` (proof `simple`).
- **REQ-2**: WHEN `because` is present without becauseArgs THE SYSTEM SHALL pass it as customMessage to `Should.Throw` (proof `because`).
- **REQ-3**: WHEN `.WithMessage(p)` follows THE SYSTEM SHALL put the message check of `fa.throw` on the `ShouldBeOfType` result (proof `message, async-message`).
- **REQ-4**: WHEN `await act.Should().ThrowExactlyAsync<T>()` is present THE SYSTEM SHALL produce `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()` (proof `async`).
- **REQ-5**: WHEN the action uses `Wait()`/`.Result` THE SYSTEM SHALL still rewrite: ThrowExactly does not unwrap AggregateException, and neither does Shouldly (proof `aggregate-wait`).
- **REQ-6**: IF becauseArgs, an AssertionScope or a used result THEN THE SYSTEM SHALL produce Manual (proof `because-args, assertion-scope, value-used`).
- **Extension TAAK-097**: WHEN becauseArgs accompany a constant, formattable `because` THE SYSTEM SHALL produce `Should.Throw<T>(act, string.Format(CultureInfo.InvariantCulture, because, args)).ShouldBeOfType<T>()` (proof `because-args`, now `.after.cs`); named arguments or a non-constant `because` stay Manual.
- **Extension TAAK-096**: WHEN `.Which` follows directly THE SYSTEM SHALL drop `.Which`: `ShouldBeOfType<T>()` returns the exception (proof `value-used`, now `.after.cs`). Together with TAAK-097, only the AssertionScope remains Manual from REQ-6.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0, scratchpad `probe/`): exact type, derived type, base type, no exception and AggregateException give the same outcome (pass/fail) in both, sync and async.

## Risks

- With a derived type Shouldly fails in `ShouldBeOfType` instead of in `Throw`; the message differs, the outcome does not. The `because` is only on `Should.Throw`.

## Models

Class diagram, sequence diagram and decision tree: [fa-throw-exactly-modellen.md](fa-throw-exactly-modellen.md).
