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

> Opgesteld uit TAAK-026.

## One-sentence summary

mediatr.notification-handler: in klassen die `MediatR.INotificationHandler<T>` implementeren wordt `Task Handle(...)` `ValueTask Handle(...)` en `Task.CompletedTask` `ValueTask.CompletedTask`.

## Scope (in / out)

**In scope**

- `Handle` via `MediatRSyntax.FindHandleMethods`; meerdere handlers per bestand.
- `async` (blok of expression body): alleen het returntype. Niet-async: returntype en elke `Task.CompletedTask`.

**Out of scope**

- Publisher-strategie (`TaskWhenAllPublisher`): hoort bij `mediatr.add-mediatr` (rij 5).
- Andere `Task`s en expliciete interface-implementaties: Manual.

## Requirements

- **REQ-1**: WHEN `Handle` `async Task` is THE SYSTEM SHALL alleen het returntype `ValueTask` maken (proofs `async`, `two-handlers`).
- **REQ-2**: WHEN een niet-async `Handle` alleen `Task.CompletedTask` teruggeeft THE SYSTEM SHALL `ValueTask` + `ValueTask.CompletedTask` schrijven (proofs `completed-task`, `completed-task-expression`).
- **REQ-3**: WHEN een bestand meerdere handlers voor één notification heeft THE SYSTEM SHALL elk herschrijven (proof `two-handlers`).
- **REQ-4**: IF `Handle` een andere `Task` teruggeeft of expliciet geïmplementeerd is THEN THE SYSTEM SHALL Manual melden (proofs `other-return`, `explicit`).

## Assumptions

- Doel-framework .NET 5+ (`ValueTask.CompletedTask`).

## Risks

- Geen bekende.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-notification-handler-modellen.md](mediatr-notification-handler-modellen.md).

## Open questions

- Geen.
