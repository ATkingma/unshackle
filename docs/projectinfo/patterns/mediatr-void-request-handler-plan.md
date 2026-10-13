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

> Opgesteld uit TAAK-025.

## One-sentence summary

mediatr.void-request-handler: in klassen die `MediatR.IRequestHandler<TReq>` implementeren wordt `Task Handle(...)` `ValueTask<Unit> Handle(...)`, omdat Mediator 3.0.2 `IRequestHandler<TReq>` als `IRequestHandler<TReq, Unit>` definieert; elke uitgang geeft `Unit.Value` (async) of `Unit.ValueTask` (niet-async).

## Scope (in / out)

**In scope**

- `Handle` gevonden via het semantisch model (gedeelde `MediatRSyntax.FindHandleMethods`, ook gebruikt door `mediatr.request-handler`).
- `async` met blok-body: kale `return;` → `return Unit.Value;`, en `return Unit.Value;` achteraan als het einde bereikbaar is (`AnalyzeControlFlow`).
- Niet-async: elke `Task.CompletedTask` → `Unit.ValueTask`.

**Out of scope**

- `IRequest` zelf: heet in Mediator hetzelfde, de swap via `SwappedTypes` dekt het.
- `async` met expression body, andere `Task`s (bv. `Task.Delay`), expliciete interface-implementaties: Manual.

## Requirements

- **REQ-1**: WHEN `Handle` `async Task` is met een bereikbaar einde THE SYSTEM SHALL `ValueTask<Unit>` schrijven en `return Unit.Value;` toevoegen (proof `async`).
- **REQ-2**: WHEN een `async` `Handle` kale `return;`-statements heeft THE SYSTEM SHALL die `return Unit.Value;` maken (proof `early-return`).
- **REQ-3**: WHEN het einde van de body onbereikbaar is (bv. `throw`) THE SYSTEM SHALL geen return toevoegen (proof `throw-at-end`).
- **REQ-4**: WHEN een niet-async `Handle` alleen `Task.CompletedTask` teruggeeft THE SYSTEM SHALL `ValueTask<Unit>` + `Unit.ValueTask` schrijven (proofs `completed-task`, `completed-task-expression`).
- **REQ-5**: IF `Handle` een andere `Task` teruggeeft, async is met expression body, of expliciet geïmplementeerd is THEN THE SYSTEM SHALL Manual melden (proofs `other-return`, `async-expression`, `explicit`).
- **REQ-6** (TAAK-210): THE SYSTEM SHALL `using Mediator;` (en `System.Threading.Tasks`) eisen voor de herschreven `Handle`, zodat `Unit` en `ValueTask<Unit>` ook binden in een bestand dat MediatR alleen via een eigen marker-interface zag (geen `using MediatR`) (proof `_Combined/generic-decorator-marker-without-using`, project `Handlers`).
- **REQ-7** (TAAK-210): WHEN de toegevoegde `return Unit.Value;` volgt op een statement dat op `}` eindigt THE SYSTEM SHALL er een lege regel voor zetten (StyleCop SA1513, kgrzybek) (proof `block-end`).
- **Uitbreiding TAAK-080**: WHEN de handler `IRequestHandler<T>` via een eigen marker-interface implementeert (`IAppCommandHandler<T> : IRequestHandler<T>`) THE SYSTEM SHALL hem hetzelfde herschrijven (proof `edge-marker-interface`).

## Assumptions

- Mediator 3.0.2 heeft `IRequestHandler<TRequest>` met `ValueTask<Unit> Handle` en `Unit.ValueTask`; bewezen doordat de `after`-proofs compileren tegen `Mediator.Abstractions` 3.0.2.

## Risks

- Een `async` expression body (`=> await x;`) wordt Manual; een blok maken zou trivia en opmaak raden.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-void-request-handler-modellen.md](mediatr-void-request-handler-modellen.md).

## Open questions

- Geen.
