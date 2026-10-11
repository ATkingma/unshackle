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

> Opgesteld uit TAAK-061.

## One-sentence summary

`IRequestExceptionHandler<TReq, TRes, TEx>` met `Task Handle(request, exception, state, ct)` wordt `MessageExceptionHandler<TReq, TRes, TEx>` met `protected override ValueTask<ExceptionHandlingResult<TRes>> Handle(request, exception, ct)`; `state.SetHandled(x); return Task.CompletedTask;` wordt `return new(Handled(x));`, elke andere return `return new(NotHandled);`.

## Scope (in / out)

**Out of scope**: `async` handlers, expression bodies en `IRequestExceptionAction` (blijft `mediatr.unsupported`).

## Requirements

- **REQ-1**: WHEN elk gebruik van `state` een `state.SetHandled(x);` direct gevolgd door `return Task.CompletedTask;` is THE SYSTEM SHALL `new(Handled(x))` en voor de overige returns `new(NotHandled)` schrijven (proof `set-handled`).
- **REQ-2**: IF `state` anders gebruikt wordt (doorgegeven, opgeslagen) THEN THE SYSTEM SHALL Manual melden (proof `state-passed`).

## Assumptions

- Mediator 3.0.2: `abstract class MessageExceptionHandler<TMessage, TResponse, TException>` met `protected abstract ValueTask<ExceptionHandlingResult<TResponse>> Handle(TMessage, TException, CancellationToken)`, `protected static Handled(TResponse)` en `protected static readonly NotHandled`. Niet afgehandeld = opnieuw gooien, net als MediatR.
- De registratie volgt in `mediatr.add-mediatr`; de fix is alles-of-niets (`IFix.AllOrNothing`), dus een Manual elders laat dit patroon niets schrijven.

## Modellen

Klassen, sequence en beslisboom: [mediatr-exception-handler-modellen.md](mediatr-exception-handler-modellen.md).

## Zie ook

- [mediatr-exception-handler-verify.md](mediatr-exception-handler-verify.md)
- `claude/tasks/archief/TAAK-061-*.md`
