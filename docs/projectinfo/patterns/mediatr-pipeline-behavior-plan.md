---
kingmadoc: 1
feature: "mediatr-pipeline-behavior"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/PipelineBehavior/
---

# Feature: mediatr.pipeline-behavior: IPipelineBehavior → Mediator IPipelineBehavior

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-027.

## One-sentence summary

mediatr.pipeline-behavior: `Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)` becomes `ValueTask<TRes> Handle(TReq request, MessageHandlerDelegate<TReq, TRes> next, CancellationToken ct)`, `next()` becomes `next(request, ct)` and an open request type parameter gets `IMessage` as a constraint.

## Scope (in / out)

**In scope**

- Open (`LoggingBehavior<TReq, TRes>`) and closed (`IPipelineBehavior<Cmd, Unit>`) behaviors.
- `async` and non-async; non-async only if every return is `next(...)` or `Task.FromResult(...)`.
- `next()` and `next(token)` (MediatR 12.5 has an optional token parameter) → `next(request, token)`.
- `IPipelineBehavior\`2`in`MediatRFix.SwappedTypes`: the name stays, the using becomes `Mediator`.

**Out of scope**

- `IStreamPipelineBehavior`: row 7 (`mediatr.unsupported`).
- Registration (`AddOpenBehavior`): row 5.

## Requirements

- **REQ-1**: WHEN an open behavior is `async` THE SYSTEM SHALL rewrite the return type, delegate and `next` calls and add `IMessage` to the existing constraint (proof `open-async`).
- **REQ-2**: WHEN the request type parameter has no constraint THE SYSTEM SHALL add `where TRequest : IMessage`; with `new()`, `IMessage` goes before it (proofs `no-constraint`, `new-constraint`).
- **REQ-3**: WHEN a closed behavior non-async returns `next()` or `Task.FromResult` THE SYSTEM SHALL write `next(request, token)` and `ValueTask.FromResult`, without a constraint (proof `closed`).
- **REQ-4**: WHEN `next(token)` is called THE SYSTEM SHALL write `next(request, token)` (proof `next-token`).
- **REQ-5**: IF `next` is used other than being called, a non-async return is something else, or `Handle` is implemented explicitly THEN THE SYSTEM SHALL report Manual (proofs `next-stored`, `next-passed`, `explicit`).
- **Extension TAAK-081**: WHEN the constraint of the request type parameter is already `IMessage` after the swap (`IRequest<TResponse>`, `INotification` or an own interface on top of it) THE SYSTEM SHALL not add an extra `IMessage` (proof `edge-request-constraint`); another constraint such as `notnull` gets `IMessage` after it (proof `edge-notnull-constraint`).

## Assumptions

- Mediator 3.0.2: `IPipelineBehavior<TMessage, TResponse> where TMessage : IMessage`, `ValueTask<TResponse> Handle(TMessage, MessageHandlerDelegate<TMessage, TResponse>, CancellationToken)`; `MessageHandlerDelegate.Invoke(TMessage, CancellationToken)` (checked with reflection on the real assembly).
- After the swap, a closed request type implements `Mediator.IRequest*` and therefore `IMessage`.

## Risks

- On Manual, `IPipelineBehavior` keeps pointing to Mediator because of the swap; `RequestHandlerDelegate` then holds on to the MediatR using and the package (Manual finding project-file). Same behavior as the handler patterns.

## Models

Class diagram, sequence and decision tree: [mediatr-pipeline-behavior-modellen.md](mediatr-pipeline-behavior-modellen.md).

## Open questions

- None.
