---
kingmadoc: 1
feature: "fa-not-throw-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowAsyncPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/NotThrowAsync/
---

# Feature: fa.not-throw-async

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-094 (action from `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`await act.Should().NotThrowAsync()` becomes `await Should.NotThrowAsync(act)`, with `because` and `.ConfigureAwait(..)`.

## Scope (in / out)

**In scope**

- `await act.Should().NotThrowAsync([because])` as a statement, also with `.ConfigureAwait(..)`.
- Subject `Func<Task>` or `Func<Task<T>>` with T a reference type.

**Out of scope**

- `NotThrowAsync<T>()`, `NotThrowAfterAsync`, a non-awaited or used result, `Func<Task<int>>`, `FluentActions.Awaiting`, AssertionScope: Manual. (becauseArgs: see Extension TAAK-097.)

## Requirements

- **REQ-1**: WHEN `await act.Should().NotThrowAsync()` is a statement THE SYSTEM SHALL give `await Should.NotThrowAsync(act)` (proof `simple, lambda, reference-result`).
- **REQ-2**: WHEN there is a `because` without becauseArgs THE SYSTEM SHALL pass it as customMessage (proof `because`).
- **REQ-3**: WHEN `.ConfigureAwait(x)` follows THE SYSTEM SHALL keep it on `Should.NotThrowAsync` (proof `configure-await`).
- **REQ-4**: IF generic, not awaited, result used, value Task, FA subject or AssertionScope THEN THE SYSTEM SHALL give Manual (proof `generic, not-awaited, result-used, value-result, awaiting, assertion-scope`).
- **Extension TAAK-173**: WHEN the subject is `x.Awaiting(s => …)` or `FluentActions.Awaiting(d)` THE SYSTEM SHALL build the action like `fa.invoking` (`ThrowSyntax.TryGetAction`: stable receiver, lambda/method group/delegate variable) and give `await Should.NotThrowAsync(() => …)`; a written lambda may return a `Task<T>` with a value type (proofs `awaiting`, `awaiting-receiver`, `fluent-actions-lambda`, `awaiting-value-result`); a receiver with side effects stays Manual (`awaiting-side-effect`). Replaces the FA-subject part of REQ-4.
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (Fixed, proof `because-args`); named arguments or a non-constant `because` stay Manual.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0): both pass with a completed task and fail with a synchronous throw, an async throw and an AggregateException (Shouldly throws the original exception on a synchronous throw; the test is red in both).
- Therefore the AggregateException heuristic of `ThrowMessage.MayThrowAggregate` is not needed here: NotThrow fails on every exception.
- Shouldly 4.3.0 (reflection): `Should.NotThrowAsync(Func<Task>, string?)` and `(Task, string?)`.

## Risks

- The error message on a synchronous throw is the original exception instead of an assertion message.

## Models

Class diagram, sequence diagram and decision tree: [fa-not-throw-async-modellen.md](fa-not-throw-async-modellen.md).

## See also

- [fa-not-throw-async-verify.md](fa-not-throw-async-verify.md)
- `claude/tasks/archief/TAAK-094-fa-not-throw-async.md`
- [../README.md](../README.md)
