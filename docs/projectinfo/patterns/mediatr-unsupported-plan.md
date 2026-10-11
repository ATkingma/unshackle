---
kingmadoc: 1
feature: "mediatr-unsupported"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/Unsupported/
---

# Feature: mediatr.unsupported: MediatR-typen zonder tweeling → Manual met advies

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-037.

## One-sentence summary

mediatr.unsupported: typen die een MediatR-interface implementeren zonder 1-op-1-tegenhanger in Mediator 3.0.2 worden Manual, met in de reden wat je in Mediator schrijft.

## Scope (in / out)

**In scope (Manual met advies)**

- `IRequestPreProcessor<T>` → `MessagePreProcessor<TMessage, TResponse>`; `IRequestPostProcessor<T, R>` → `MessagePostProcessor<TMessage, TResponse>`.
- `IRequestExceptionHandler<T, R, E>` en `IRequestExceptionAction<T, E>` → `MessageExceptionHandler<TMessage, TResponse, TException>`.
- `INotificationPublisher` (andere `Publish`-vorm) → herschrijven + `NotificationPublisherType`.
- `IStreamPipelineBehavior<T, R>` → `IStreamMessage`-constraint, `next(message, ct)`, `StreamPipelineBehaviors`.
- Generieke berichten en request-handlers (`CountQuery<T> : IRequest<int>`, `CountHandler<T>`): de generator ondersteunt ze niet.

**Out of scope (er is een tweeling, dus geen Manual)**

- `IStreamRequest<T>` en `IStreamRequestHandler<T, R>`: zelfde vorm in Mediator; nu in `MediatRFix.SwappedTypes`.
- `Send(object)`/`Publish(object)`: bestaan in Mediator (`ValueTask<object?>`); `mediatr.send-as-task` dekt Task-gebruik.
- Generieke notification-handlers: Mediator registreert open generics zelf.

## Requirements

- **REQ-1**: WHEN een type `IRequestPreProcessor` implementeert THE SYSTEM SHALL Manual melden met `MessagePreProcessor` (proof `pre-processor`).
- **REQ-2**: WHEN een type `IRequestPostProcessor` implementeert THE SYSTEM SHALL Manual melden met `MessagePostProcessor` (proof `post-processor`).
- **REQ-3**: WHEN een type `IRequestExceptionHandler` of `IRequestExceptionAction` implementeert THE SYSTEM SHALL Manual melden met `MessageExceptionHandler` (proof `exception-handler`).
- **REQ-4**: WHEN een type MediatR's `INotificationPublisher` implementeert THE SYSTEM SHALL Manual melden met `NotificationPublisherType` (proof `notification-publisher`).
- **REQ-5**: WHEN een type `IStreamPipelineBehavior` implementeert THE SYSTEM SHALL Manual melden met `StreamPipelineBehaviors` (proof `stream-behavior`).
- **REQ-6**: WHEN een generiek type een request, notification of request-handler is THE SYSTEM SHALL Manual melden (proof `generic-handler`).

## Assumptions

- Typen in Mediator 3.0.2 gecontroleerd in `Mediator.dll`/`Mediator.xml`. Generieke berichten: README ("doesn't support generic requests/notifications") en generator-melding "request/query/command handlers cannot be generic"; op een kopie van SampleShop gaf `CountQuery<TEntity>` CS0246 in `Mediator.g.cs`.

## Risks

- Advies is tekst, geen herschrijving; de gebruiker moet ook `AddMediator` bijwerken.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-unsupported-modellen.md](mediatr-unsupported-modellen.md).

## Open questions

- Geen.
