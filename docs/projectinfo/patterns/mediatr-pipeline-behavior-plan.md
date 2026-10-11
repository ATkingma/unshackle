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

> Opgesteld uit TAAK-027.

## One-sentence summary

mediatr.pipeline-behavior: `Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)` wordt `ValueTask<TRes> Handle(TReq request, MessageHandlerDelegate<TReq, TRes> next, CancellationToken ct)`, `next()` wordt `next(request, ct)` en een open request-typeparameter krijgt `IMessage` als constraint.

## Scope (in / out)

**In scope**

- Open (`LoggingBehavior<TReq, TRes>`) en gesloten (`IPipelineBehavior<Cmd, Unit>`) behaviors.
- `async` en niet-async; niet-async alleen als elke return `next(...)` of `Task.FromResult(...)` is.
- `next()` en `next(token)` (MediatR 12.5 heeft een optionele token-parameter) → `next(request, token)`.
- `IPipelineBehavior\`2`in`MediatRFix.SwappedTypes`: de naam blijft, de using wordt `Mediator`.

**Out of scope**

- `IStreamPipelineBehavior`: rij 7 (`mediatr.unsupported`).
- Registratie (`AddOpenBehavior`): rij 5.

## Requirements

- **REQ-1**: WHEN een open behavior `async` is THE SYSTEM SHALL returntype, delegate en `next`-aanroepen herschrijven en `IMessage` toevoegen aan de bestaande constraint (proof `open-async`).
- **REQ-2**: WHEN de request-typeparameter geen constraint heeft THE SYSTEM SHALL `where TRequest : IMessage` toevoegen; met `new()` komt `IMessage` ervoor (proofs `no-constraint`, `new-constraint`).
- **REQ-3**: WHEN een gesloten behavior niet-async `next()` of `Task.FromResult` teruggeeft THE SYSTEM SHALL `next(request, token)` en `ValueTask.FromResult` schrijven, zonder constraint (proof `closed`).
- **REQ-4**: WHEN `next(token)` wordt aangeroepen THE SYSTEM SHALL `next(request, token)` schrijven (proof `next-token`).
- **REQ-5**: IF `next` anders gebruikt wordt dan aangeroepen, een niet-async return iets anders is, of `Handle` expliciet geïmplementeerd is THEN THE SYSTEM SHALL Manual melden (proofs `next-stored`, `next-passed`, `explicit`).
- **Uitbreiding TAAK-081**: WHEN de constraint van de request-typeparameter na de swap al `IMessage` is (`IRequest<TResponse>`, `INotification` of een eigen interface daarop) THE SYSTEM SHALL geen extra `IMessage` toevoegen (proof `edge-request-constraint`); een andere constraint zoals `notnull` krijgt `IMessage` erachter (proof `edge-notnull-constraint`).

## Assumptions

- Mediator 3.0.2: `IPipelineBehavior<TMessage, TResponse> where TMessage : IMessage`, `ValueTask<TResponse> Handle(TMessage, MessageHandlerDelegate<TMessage, TResponse>, CancellationToken)`; `MessageHandlerDelegate.Invoke(TMessage, CancellationToken)` (gecontroleerd met reflectie op de echte assembly).
- Een gesloten request-type implementeert na de swap `Mediator.IRequest*` en dus `IMessage`.

## Risks

- Bij Manual blijft `IPipelineBehavior` door de swap naar Mediator wijzen; `RequestHandlerDelegate` houdt dan de MediatR-using en het pakket vast (Manual-bevinding project-file). Zelfde gedrag als de handler-patronen.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-pipeline-behavior-modellen.md](mediatr-pipeline-behavior-modellen.md).

## Open questions

- Geen.
