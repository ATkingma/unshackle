---
kingmadoc: 1
feature: "mediatr-void-request-handler"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/VoidRequestHandlerPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/VoidRequestHandler/
---

# Feature: mediatr.void-request-handler: Task Handle → ValueTask<Unit> Handle

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-025.

## One-sentence summary

mediatr.void-request-handler: in classes that implement `MediatR.IRequestHandler<TReq>`, `Task Handle(...)` becomes `ValueTask<Unit> Handle(...)`, because Mediator 3.0.2 defines `IRequestHandler<TReq>` as `IRequestHandler<TReq, Unit>`; every exit returns `Unit.Value` (async) or `Unit.ValueTask` (non-async).

## Scope (in / out)

**In scope**

- `Handle` found via the semantic model (shared `MediatRSyntax.FindHandleMethods`, also used by `mediatr.request-handler`).
- `async` with a block body: a bare `return;` → `return Unit.Value;`, and `return Unit.Value;` at the end if the end is reachable (`AnalyzeControlFlow`).
- Non-async: every `Task.CompletedTask` → `Unit.ValueTask`.

**Out of scope**

- `IRequest` itself: has the same name in Mediator, the swap via `SwappedTypes` covers it.
- `async` with an expression body, other `Task`s (e.g. `Task.Delay`), explicit interface implementations: Manual.

## Requirements

- **REQ-1**: WHEN `Handle` is `async Task` with a reachable end THE SYSTEM SHALL write `ValueTask<Unit>` and add `return Unit.Value;` (proof `async`).
- **REQ-2**: WHEN an `async` `Handle` has bare `return;` statements THE SYSTEM SHALL make them `return Unit.Value;` (proof `early-return`).
- **REQ-3**: WHEN the end of the body is unreachable (e.g. `throw`) THE SYSTEM SHALL add no return (proof `throw-at-end`).
- **REQ-4**: WHEN a non-async `Handle` only returns `Task.CompletedTask` THE SYSTEM SHALL write `ValueTask<Unit>` + `Unit.ValueTask` (proofs `completed-task`, `completed-task-expression`).
- **REQ-5**: IF `Handle` returns another `Task`, is async with an expression body, or is implemented explicitly THEN THE SYSTEM SHALL report Manual (proofs `other-return`, `async-expression`, `explicit`).
- **REQ-6** (TAAK-210): THE SYSTEM SHALL require `using Mediator;` (and `System.Threading.Tasks`) for the rewritten `Handle`, so that `Unit` and `ValueTask<Unit>` also bind in a file that only saw MediatR via an own marker interface (no `using MediatR`) (proof `_Combined/generic-decorator-marker-without-using`, project `Handlers`).
- **REQ-7** (TAAK-210): WHEN the added `return Unit.Value;` follows a statement that ends in `}` THE SYSTEM SHALL put a blank line before it (StyleCop SA1513, kgrzybek) (proof `block-end`).
- **Extension TAAK-080**: WHEN the handler implements `IRequestHandler<T>` via an own marker interface (`IAppCommandHandler<T> : IRequestHandler<T>`) THE SYSTEM SHALL rewrite it the same way (proof `edge-marker-interface`).

## Assumptions

- Mediator 3.0.2 has `IRequestHandler<TRequest>` with `ValueTask<Unit> Handle` and `Unit.ValueTask`; proven because the `after` proofs compile against `Mediator.Abstractions` 3.0.2.

## Risks

- An `async` expression body (`=> await x;`) becomes Manual; making a block would guess trivia and formatting.

## Models

Class diagram, sequence and decision tree: [mediatr-void-request-handler-modellen.md](mediatr-void-request-handler-modellen.md).

## Open questions

- None.
