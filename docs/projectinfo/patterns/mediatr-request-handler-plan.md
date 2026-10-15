---
kingmadoc: 1
feature: "mediatr-request-handler"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestHandler/
---

# Feature: mediatr.request-handler: Task<T> Handle → ValueTask<T> Handle

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-023.

## One-sentence summary

mediatr.request-handler: in classes that implement `MediatR.IRequestHandler<TReq,TRes>`, `Task<TRes> Handle(...)` becomes `ValueTask<TRes> Handle(...)` and `Task.FromResult(x)` becomes `ValueTask.FromResult(x)`; the using swap of `MediatRFix.SwappedTypes` does the rest.

## Scope (in / out)

**In scope**

- `Handle` found via the semantic model (`FindImplementationForInterfaceMember`), also in generic handlers.
- `async`: only the return type. Non-async: return type and every `Task.FromResult` (expression body or block).

**Out of scope**

- `IRequestHandler<TReq>` (void), notification handlers, pipeline behaviors: own patterns (rows 2–4).
- `Task.Run`, passed-through `Task`s: Manual. An explicit `IRequestHandler<,>.Handle` keeps its specifier and only gets the `ValueTask<T>` return type (TAAK-270).

## Requirements

- **REQ-1**: WHEN `Handle` is `async Task<T>` THE SYSTEM SHALL only make the return type `ValueTask<T>` (proof `async`).
- **REQ-2**: WHEN `Handle` is non-async and the expression body is `Task.FromResult(x)` THE SYSTEM SHALL write `ValueTask<T>` + `ValueTask.FromResult(x)` (proof `from-result-expression`).
- **REQ-3**: WHEN every `return` in the body is `Task.FromResult(...)`, also with a type argument THE SYSTEM SHALL make them all `ValueTask.FromResult(...)` (proof `from-result-block`).
- **REQ-4**: WHEN the handler is generic THE SYSTEM SHALL rewrite it the same way (proof `generic`).
- **REQ-5**: WHEN the `CancellationToken` parameter has a different name THE SYSTEM SHALL leave the parameters untouched (proof `token-name`).
- **REQ-6**: IF a non-async `Handle` returns something other than `Task.FromResult`, THEN THE SYSTEM SHALL report Manual (proof `other-return`). **REQ-7**: WHEN `Handle` is an explicit interface implementation THE SYSTEM SHALL rewrite it like an implicit one, keeping `IRequestHandler<,>.` (proofs `explicit`, `explicit-async`).
- **REQ-7**: WHEN a non-async `Handle` (e.g. a decorator) returns `inner.Handle(...)` of an `IRequestHandler<,>` (or an own derived interface) THE SYSTEM SHALL leave that return untouched: after the swap the inner handler itself returns a `ValueTask<T>` (proof `decorator-inner-handle`, TAAK-212; was CS1503 in kgrzybek).
- **Extension TAAK-080**: WHEN the handler implements `IRequestHandler<,>` via an own marker interface (`IAppQueryHandler<TQ, TR> : IRequestHandler<TQ, TR>`) THE SYSTEM SHALL rewrite it the same way (`MediatRSyntax.FindHandleMethods` looks at `AllInterfaces`; proof `edge-marker-interface`).

## Assumptions

- Target framework .NET 5+ (`ValueTask.FromResult`).
- `using MediatR` → `using Mediator` comes from the Fixer via `SwappedTypes` (TAAK-024).

## Risks

- Files with other MediatR types as well (e.g. `INotificationHandler` swapped, `IPipelineBehavior` not) keep `using MediatR` until their pattern exists.

## Models

Class diagram, sequence and decision tree: [mediatr-request-handler-modellen.md](mediatr-request-handler-modellen.md).

## Open questions

- None.
