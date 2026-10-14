---
kingmadoc: 1
feature: "mediatr-send-as-task"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/SendAsTask/
---

# Feature: mediatr.send-as-task: Send/Publish as Task → .AsTask()

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-029.

## One-sentence summary

mediatr.send-as-task: a MediatR `Send`/`Publish` whose result is not directly `await`ed gets `.AsTask()`, because Mediator returns `ValueTask`.

## Scope (in / out)

**In scope**

- `ISender`/`IPublisher`/`IMediator` `.Send(...)` and `.Publish(...)`, also `Send(object)`/`Publish(object)`, if the symbol comes from assembly `MediatR`.
- Every use that is not a direct `await`: return in a Task method, `Func<Task>` lambda, argument of `Task.WhenAll`, `.ContinueWith`, assignment.

**Out of scope**

- `await x.Send(...)` and `await x.Send(...).ConfigureAwait(...)`: work unchanged on `ValueTask`.
- `CreateStream`: returns `IAsyncEnumerable<T>` in both libraries.

## Requirements

- **REQ-1**: WHEN `Send`/`Publish` is used as a Task (return, lambda, `Task.WhenAll`, `.ContinueWith`) THE SYSTEM SHALL add `.AsTask()` (proof `task-uses`).
- **REQ-2**: WHEN the result is directly `await`ed, also with `.ConfigureAwait(...)` THE SYSTEM SHALL leave it in place (proof `task-uses`, last line).
- **REQ-3**: WHEN `Send(object)` is returned as a Task THE SYSTEM SHALL add `.AsTask()` (proof `send-object`).
- **REQ-4**: THE SYSTEM SHALL never report Manual: `.AsTask()` preserves all Task semantics.
- **Extension review B L4**: IF `Send`/`Publish` is called with `?.` (`_sender?.Send(x)`) THEN THE SYSTEM SHALL report Manual: the null case must be handled by hand (proof `null-conditional`). This is the only exception to REQ-4.
- **REQ-5** (TAAK-211): WHEN `Send`/`Publish` is the receiver of NSubstitute `Returns`/`ReturnsForAnyArgs` THE SYSTEM SHALL not add `.AsTask()` (that configures nothing, NS1000) but move the values along: `Task.FromResult(x)` → `ValueTask.FromResult(x)` (also with type argument or qualified), `Task.CompletedTask` → `ValueTask.CompletedTask` (only `Publish`/non-generic); any other value (lambda, variable, `Task.CompletedTask` on a void `Send`, which yields `ValueTask<Unit>` in Mediator) → Manual (proofs `nsubstitute-returns`, `nsubstitute-returns-lambda`). Second exception to REQ-4.
- **REQ-6** (TAAK-211): WHEN `Send`/`Publish` is the receiver of NSubstitute `Throws*`, or stands as a standalone statement after `Received()`/`DidNotReceive()` THE SYSTEM SHALL leave it untouched (proof `nsubstitute-returns`, unit test `Find_NSubstituteThrowsSetupAndReceivedCheck_FindsNothing`). Moq `Setup` lambdas are out of scope.

## Assumptions

- Mediator 3.0.2 `ISender.Send` returns `ValueTask<T>` (also `Send(object)` → `ValueTask<object?>`), `IPublisher.Publish` returns `ValueTask`. Checked in `Mediator.xml`.

## Risks

- `var t = sender.Send(...)` also gets `.AsTask()` (small allocation); that is safe, because `t` remains a Task.

## Models

Class diagram, sequence and decision tree: [mediatr-send-as-task-modellen.md](mediatr-send-as-task-modellen.md).

## Open questions

- None.
