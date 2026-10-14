---
kingmadoc: 1
feature: "mediatr-notification-handler"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/NotificationHandlerPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/NotificationHandler/
---

# Feature: mediatr.notification-handler: Task Handle → ValueTask Handle

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-026.

## One-sentence summary

mediatr.notification-handler: in classes that implement `MediatR.INotificationHandler<T>`, `Task Handle(...)` becomes `ValueTask Handle(...)` and `Task.CompletedTask` becomes `ValueTask.CompletedTask`.

## Scope (in / out)

**In scope**

- `Handle` via `MediatRSyntax.FindHandleMethods`; several handlers per file.
- `async` (block or expression body): only the return type. Non-async: return type and every `Task.CompletedTask`.

**Out of scope**

- Publisher strategy (`TaskWhenAllPublisher`): belongs to `mediatr.add-mediatr` (row 5).
- Other `Task`s and explicit interface implementations: Manual.

## Requirements

- **REQ-1**: WHEN `Handle` is `async Task` THE SYSTEM SHALL only make the return type `ValueTask` (proofs `async`, `two-handlers`).
- **REQ-2**: WHEN a non-async `Handle` only returns `Task.CompletedTask` THE SYSTEM SHALL write `ValueTask` + `ValueTask.CompletedTask` (proofs `completed-task`, `completed-task-expression`).
- **REQ-3**: WHEN a file has several handlers for one notification THE SYSTEM SHALL rewrite each (proof `two-handlers`).
- **REQ-4**: IF `Handle` returns another `Task` or is implemented explicitly THEN THE SYSTEM SHALL report Manual (proofs `other-return`, `explicit`).
- **Extension TAAK-080**: WHEN the handler implements `INotificationHandler<T>` via an own marker interface (`IAppEventHandler<T> : INotificationHandler<T>`) THE SYSTEM SHALL rewrite it the same way (proof `edge-marker-interface`).

## Assumptions

- Target framework .NET 5+ (`ValueTask.CompletedTask`).

## Risks

- None known.

## Models

Class diagram, sequence and decision tree: [mediatr-notification-handler-modellen.md](mediatr-notification-handler-modellen.md).

## Open questions

- None.
