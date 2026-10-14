---
kingmadoc: 1
feature: "mediatr-exception-handler"
status: approved
requirements: [REQ-1, REQ-2]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/ExceptionHandlerPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/ExceptionHandler/
---

# Feature: mediatr.exception-handler: IRequestExceptionHandler → MessageExceptionHandler

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-061.

## One-sentence summary

`IRequestExceptionHandler<TReq, TRes, TEx>` with `Task Handle(request, exception, state, ct)` becomes `MessageExceptionHandler<TReq, TRes, TEx>` with `protected override ValueTask<ExceptionHandlingResult<TRes>> Handle(request, exception, ct)`; `state.SetHandled(x); return Task.CompletedTask;` becomes `return new(Handled(x));`, every other return `return new(NotHandled);`.

## Scope (in / out)

**Out of scope**: `async` handlers, expression bodies and `IRequestExceptionAction` (stays `mediatr.unsupported`).

## Requirements

- **REQ-1**: WHEN every use of `state` is a `state.SetHandled(x);` directly followed by `return Task.CompletedTask;` THE SYSTEM SHALL write `new(Handled(x))` and `new(NotHandled)` for the other returns (proof `set-handled`).
- **REQ-2**: IF `state` is used otherwise (passed on, stored) THEN THE SYSTEM SHALL report Manual (proof `state-passed`).

## Assumptions

- Mediator 3.0.2: `abstract class MessageExceptionHandler<TMessage, TResponse, TException>` with `protected abstract ValueTask<ExceptionHandlingResult<TResponse>> Handle(TMessage, TException, CancellationToken)`, `protected static Handled(TResponse)` and `protected static readonly NotHandled`. Not handled = rethrow, just like MediatR.
- The registration follows in `mediatr.add-mediatr`; the fix is all-or-nothing (`IFix.AllOrNothing`), so a Manual elsewhere makes this pattern write nothing.

## Models

Classes, sequence and decision tree: [mediatr-exception-handler-modellen.md](mediatr-exception-handler-modellen.md).

## See also

- [mediatr-exception-handler-verify.md](mediatr-exception-handler-verify.md)
- `claude/tasks/archief/TAAK-061-*.md`
