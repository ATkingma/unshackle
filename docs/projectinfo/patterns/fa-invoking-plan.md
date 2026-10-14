---
kingmadoc: 1
feature: "fa-invoking"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/
---

# Feature: fa.invoking

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-052.

## One-sentence summary

`x.Invoking(s => s.M()).Should().Throw<T>()` becomes `Should.Throw<T>(() => x.M())`; also `Awaiting`/`ThrowAsync`, `ThrowExactly(Async)` and `NotThrow`.

## Scope (in / out)

**In scope**

- `Invoking`/`Awaiting` with a one-parameter lambda on a local, parameter, field or `this`, followed by `Throw`, `ThrowAsync`, `ThrowExactly(Async)` or `NotThrow`.

**Out of scope**

- Receiver with possible side effects (call, property, indexer): Manual.
- Value-type receiver, method group: Manual.
- `FluentActions.Invoking(...)`: stays Manual as before.

## Requirements

- **REQ-1**: WHEN `x.Invoking(s => s.M()).Should().Throw<T>()` is present THE SYSTEM SHALL produce `Should.Throw<T>(() => x.M())` (proof `simple, func`).
- **REQ-2**: WHEN `because` or `.WithMessage` follows THE SYSTEM SHALL apply the rules of `fa.throw` (proof `because-message`).
- **REQ-3**: WHEN `ThrowExactly<T>` follows THE SYSTEM SHALL add `.ShouldBeOfType<T>()` (proof `throw-exactly, awaiting-exactly`).
- **REQ-4**: WHEN `NotThrow()` follows THE SYSTEM SHALL produce `Should.NotThrow(() => x.M())` (proof `not-throw`).
- **REQ-5**: WHEN `x.Awaiting(s => s.MAsync()).Should().ThrowAsync<T>()` is present THE SYSTEM SHALL produce `await Should.ThrowAsync<T>(() => x.MAsync())` (proof `awaiting`).
- **REQ-6**: IF the receiver can have side effects or is a value type, or the argument is not a lambda THEN THE SYSTEM SHALL give Manual (proof `side-effect-receiver, value-type, method-group`).
- **REQ-7**: IF becauseArgs, an AssertionScope or a blocking Wait in the lambda THEN THE SYSTEM SHALL give Manual (proof `because-args, assertion-scope, aggregate`). _For becauseArgs replaced by Extension TAAK-097._
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL pass `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (Fixed, proof `because-args`); named arguments or a non-constant `because` remain Manual. `InvokingPattern` now searches via `ThrowSyntax.FindThrow`/`FindThrowAsync`/`FindNotThrow` and rewrites via `ThrowSyntax.Rewrite`.
- **Extension TAAK-173**: `FluentActions.Invoking(d)`/`Awaiting(d)` return `d` itself: a lambda, or a stable `Action`/`Func<Task>` variable, goes straight to Shouldly (proof `fluent-actions`). A lambda written by the fix converts to `Action`/`Func<object?>`/`Func<Task>`, so a value-type result (`int`, enum, `Task<int>`) is Fixed too (proof `value-result`); only a passed-in delegate must have a type that Shouldly accepts.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0, scratchpad `probe/`): Invoking/Awaiting + Throw/ThrowAsync/NotThrow give the same pass/fail as the Shouldly form with `() => x.M()`; the action runs once.

## Risks

- A local that is modified between `Invoking` and execution is impossible: both happen in the same expression.
- Properties are rejected, even when they have no side effects (shortcut in the code).

## Models

Class diagram, sequence diagram and decision tree: [fa-invoking-modellen.md](fa-invoking-modellen.md).
