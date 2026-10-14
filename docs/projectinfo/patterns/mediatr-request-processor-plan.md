---
kingmadoc: 1
feature: "mediatr-request-processor"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestProcessor/
---

# Feature: mediatr.request-processor: IRequestPreProcessor / IRequestPostProcessor → MessagePreProcessor / MessagePostProcessor

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-061.

## One-sentence summary

A class that itself implements `IRequestPreProcessor<TReq>` or `IRequestPostProcessor<TReq, TRes>` becomes a subclass of `MessagePreProcessor<TReq, TRes>` / `MessagePostProcessor<TReq, TRes>`; `public Task Process(...)` becomes `protected override ValueTask Handle(...)`.

## Scope (in / out)

**Out of scope**: Abstract processors, processors with their own base class, or classes that are both pre and post: Manual. Subclasses of an own base processor are not found (the base is Manual, so all-or-nothing keeps the solution intact).

## Requirements

- **REQ-1**: WHEN an open pre-processor returns `Task.CompletedTask` THE SYSTEM SHALL add a second type parameter `TResponse`, set the base class and `IMessage` constraint and write `ValueTask.CompletedTask` (proof `pre-open`).
- **REQ-2**: WHEN a post-processor is `async` THE SYSTEM SHALL only adjust base class, modifiers, name and return type (proof `post-open-async`).
- **REQ-3**: WHEN a closed pre-processor has a request with `IRequest<T>` or `IRequest` THE SYSTEM SHALL use `T` or `Unit` respectively as the response (proof `pre-closed`).
- **REQ-4**: WHEN `Process` without async returns another `Task` (`Task.Delay(...)`, `Task.Run(...)`, expression body or block) THE SYSTEM SHALL write `return new ValueTask(expr)`; synchronous exception timing stays the same (proofs `other-return`, `task-run`; TAAK-184). IF something is returned that is not a `Task` THEN Manual.
- **REQ-5**: IF an open pre-processor already has a type parameter `TResponse` THEN THE SYSTEM SHALL report Manual. Use of the class elsewhere (`new P<Cmd>()`, `Process` calls) is handled by `mediatr.request-processor-caller` (TAAK-190); the old safety-net proof `used-elsewhere` has therefore been dropped.

## Assumptions

- Mediator 3.0.2 (ilspy on the real assembly): `abstract class MessagePreProcessor<TMessage, TResponse> where TMessage : notnull, IMessage` with `protected abstract ValueTask Handle(TMessage, CancellationToken)`; `MessagePostProcessor` likewise with `Handle(TMessage, TResponse, CancellationToken)`.
- The registration is handled in `mediatr.add-mediatr`; the fix is all-or-nothing (`IFix.AllOrNothing`), so a Manual elsewhere makes this pattern write nothing.

## Models

Classes, sequence and decision tree: [mediatr-request-processor-modellen.md](mediatr-request-processor-modellen.md).

## See also

- [mediatr-request-processor-verify.md](mediatr-request-processor-verify.md)
- `claude/tasks/archief/TAAK-061-*.md`
