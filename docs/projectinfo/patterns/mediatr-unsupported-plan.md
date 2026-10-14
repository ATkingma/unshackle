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

# Feature: mediatr.unsupported: MediatR types without a twin → Manual with advice

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-037.

## One-sentence summary

mediatr.unsupported: types that implement a MediatR interface without a 1-to-1 counterpart in Mediator 3.0.2 become Manual, with what to write in Mediator in the reason.

## Scope (in / out)

**In scope (Manual with advice)**

- `IRequestPreProcessor<T>` → `MessagePreProcessor<TMessage, TResponse>`; `IRequestPostProcessor<T, R>` → `MessagePostProcessor<TMessage, TResponse>`.
- `IRequestExceptionHandler<T, R, E>` and `IRequestExceptionAction<T, E>` → `MessageExceptionHandler<TMessage, TResponse, TException>`.
- `INotificationPublisher` (another `Publish` shape) → rewrite + `NotificationPublisherType`.
- `IStreamPipelineBehavior<T, R>` → `IStreamMessage` constraint, `next(message, ct)`, `StreamPipelineBehaviors`.
- Generic messages and request handlers (`CountQuery<T> : IRequest<int>`, `CountHandler<T>`): the generator does not support them.

**Out of scope (there is a twin, so no Manual)**

- `IStreamRequest<T>` and `IStreamRequestHandler<T, R>`: same shape in Mediator; now in `MediatRFix.SwappedTypes`.
- `Send(object)`/`Publish(object)`: exist in Mediator (`ValueTask<object?>`); `mediatr.send-as-task` covers Task usage.
- Generic notification handlers: Mediator registers open generics itself.

## Requirements

- **REQ-1**: WHEN a type implements `IRequestPreProcessor` THE SYSTEM SHALL report Manual with `MessagePreProcessor` (proof `pre-processor`).
- **REQ-2**: WHEN a type implements `IRequestPostProcessor` THE SYSTEM SHALL report Manual with `MessagePostProcessor` (proof `post-processor`).
- **REQ-3**: WHEN a type implements `IRequestExceptionHandler` or `IRequestExceptionAction` THE SYSTEM SHALL report Manual with `MessageExceptionHandler` (proof `exception-handler`).
- **REQ-4**: WHEN a type implements MediatR's `INotificationPublisher` THE SYSTEM SHALL report Manual with `NotificationPublisherType` (proof `notification-publisher`).
- **REQ-5**: WHEN a type implements `IStreamPipelineBehavior` THE SYSTEM SHALL report Manual with `StreamPipelineBehaviors` (proof `stream-behavior`).
- **REQ-6**: WHEN a generic type is a request, notification or request handler THE SYSTEM SHALL report Manual (proof `generic-handler`).
- **Extension TAAK-061/062**: REQ-1, REQ-2, REQ-3 (`IRequestExceptionHandler`) and REQ-5 are dropped here: pre/post processors, exception handlers and stream behaviors are patterns of their own (`mediatr.request-processor`, `mediatr.exception-handler`, `mediatr.stream-pipeline-behavior`). `IRequestExceptionAction` stays Manual with the `MessageExceptionHandler` advice (proof `exception-action`). Advice only on the type that declares the interface itself; a generic message is also reported if it gets `IRequest` via a base type (proof `derived-generic-message`). Interface declarations (own marker interfaces, TAAK-080) are skipped.

## Assumptions

- Types in Mediator 3.0.2 checked in `Mediator.dll`/`Mediator.xml`. Generic messages: README ("doesn't support generic requests/notifications") and the generator message "request/query/command handlers cannot be generic"; on a copy of SampleShop `CountQuery<TEntity>` gave CS0246 in `Mediator.g.cs`.

## Risks

- The advice is text, not a rewrite; the user must also update `AddMediator`.

## Models

Class diagram, sequence and decision tree: [mediatr-unsupported-modellen.md](mediatr-unsupported-modellen.md).

## Open questions

- None.
