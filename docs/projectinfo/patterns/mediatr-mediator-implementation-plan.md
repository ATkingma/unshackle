---
kingmadoc: 1
feature: "mediatr-mediator-implementation"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/MediatorImplementationPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/MediatorImplementation/
---

# Feature: mediatr.mediator-implementation: eigen no-op `IMediator` → Mediator's interface

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-13 (met de hand) |

> Opgesteld uit TAAK-197 (ardalis v9.1.0, `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` brak de build met CS0535/CS0738).

## One-sentence summary

Een eigen klasse die MediatR's `IMediator`, `ISender` of `IPublisher` implementeert en alleen no-ops bevat, krijgt de members van Mediator 3.0.2
(`ValueTask`, `Send` voor `IRequest`/`ICommand`/`IQuery`/`object`, vier `CreateStream`-overloads, twee `Publish`), elk weer als no-op.

## Scope (in / out)

**In**: klasse met `IMediator`/`ISender`/`IPublisher` van MediatR in de eigen basislijst, waarvan elke implementerende methode in die declaratie staat en alleen
voltooit: `return Task.CompletedTask;`, `return Task.FromResult(default | default! | null | default(T));`, `await Task.CompletedTask;`, `yield break;`, of leeg.

**Out (Manual)**: een member met logica, een expliciete interface-implementatie, een member uit een basisklasse of ander `partial`-deel.

## Requirements

- **REQ-1**: WHEN een no-op `IMediator` gevonden wordt THE SYSTEM SHALL de oude members vervangen door Mediator's `ISender`- en `IPublisher`-members als no-op (`=> default`, lege async iterator) op de plek van de eerste oude member; andere members blijven (proof `no-op`).
- **REQ-2**: WHEN alleen `IPublisher` (of `ISender`) geïmplementeerd wordt THE SYSTEM SHALL alleen die members schrijven en alleen de nodige usings toevoegen (proof `publisher`).
- **REQ-3**: THE SYSTEM SHALL `object?` schrijven waar nullable annotations aan staan, anders `object`.
- **REQ-4**: IF een member meer doet dan voltooien THEN Manual (proof `with-logic`).

## Assumptions

- Mediator 3.0.2: `IMediator : ISender, IPublisher` zonder eigen members; namen en defaults van parameters gemeten met reflectie (TAAK-197).
- `default` voor `ValueTask<TResponse>` is een voltooide taak met `default(TResponse)`, gelijk aan `Task.FromResult<TResponse>(default!)`.
- MediatR's `Send<TRequest>(TRequest) where TRequest : IRequest` heeft geen tegenhanger: Mediator's void-request is `IRequest<Unit>`.

## Modellen

[mediatr-mediator-implementation-modellen.md](mediatr-mediator-implementation-modellen.md).

## Zie ook

- [mediatr-mediator-implementation-verify.md](mediatr-mediator-implementation-verify.md)
- `claude/tasks/archief/TAAK-197-mediatr-eigen-imediator-implementatie.md`
