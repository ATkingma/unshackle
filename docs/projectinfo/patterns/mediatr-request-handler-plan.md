---
kingmadoc: 1
feature: "mediatr-request-handler"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
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

> Opgesteld uit TAAK-023.

## One-sentence summary

mediatr.request-handler: in klassen die `MediatR.IRequestHandler<TReq,TRes>` implementeren wordt `Task<TRes> Handle(...)` `ValueTask<TRes> Handle(...)` en `Task.FromResult(x)` `ValueTask.FromResult(x)`; de using-swap van `MediatRFix.SwappedTypes` doet de rest.

## Scope (in / out)

**In scope**

- `Handle` gevonden via het semantisch model (`FindImplementationForInterfaceMember`), ook in generieke handlers.
- `async`: alleen het returntype. Niet-async: returntype en elke `Task.FromResult` (expression body of block).

**Out of scope**

- `IRequestHandler<TReq>` (void), notification-handlers, pipeline-behaviors: eigen patronen (rijen 2–4).
- `Task.Run`, doorgegeven `Task`s, expliciete interface-implementaties: Manual.

## Requirements

- **REQ-1**: WHEN `Handle` `async Task<T>` is THE SYSTEM SHALL alleen het returntype `ValueTask<T>` maken (proof `async`).
- **REQ-2**: WHEN `Handle` niet-async is en de expression body `Task.FromResult(x)` is THE SYSTEM SHALL `ValueTask<T>` + `ValueTask.FromResult(x)` schrijven (proof `from-result-expression`).
- **REQ-3**: WHEN elke `return` in de body `Task.FromResult(...)` is, ook met type-argument THE SYSTEM SHALL ze allemaal `ValueTask.FromResult(...)` maken (proof `from-result-block`).
- **REQ-4**: WHEN de handler generiek is THE SYSTEM SHALL hem hetzelfde herschrijven (proof `generic`).
- **REQ-5**: WHEN de `CancellationToken`-parameter een andere naam heeft THE SYSTEM SHALL de parameters ongemoeid laten (proof `token-name`).
- **REQ-6**: IF een niet-async `Handle` iets anders dan `Task.FromResult` teruggeeft, of `Handle` expliciet geïmplementeerd is THEN THE SYSTEM SHALL Manual melden (proofs `other-return`, `explicit`).
- **Uitbreiding TAAK-080**: WHEN de handler `IRequestHandler<,>` via een eigen marker-interface implementeert (`IAppQueryHandler<TQ, TR> : IRequestHandler<TQ, TR>`) THE SYSTEM SHALL hem hetzelfde herschrijven (`MediatRSyntax.FindHandleMethods` kijkt naar `AllInterfaces`; proof `edge-marker-interface`).

## Assumptions

- Doel-framework .NET 5+ (`ValueTask.FromResult`).
- `using MediatR` → `using Mediator` komt van de Fixer via `SwappedTypes` (TAAK-024).

## Risks

- Bestanden met nog andere MediatR-typen (bv. `INotificationHandler` wél swapped, `IPipelineBehavior` niet) houden `using MediatR` tot hun patroon er is.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-request-handler-modellen.md](mediatr-request-handler-modellen.md).

## Open questions

- Geen.
