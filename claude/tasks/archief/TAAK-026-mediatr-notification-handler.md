# TAAK-026 — mediatr.notification-handler

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `INotificationHandler<T>` met `Task Handle(...)` (MediatR 12.5.0) → `ValueTask Handle(...)` (Mediator 3.0.2).

## Context voor de volgende agent

- Rij 3 in `claude/tasks/mediatr-queue.md`. Hergebruikt `MediatRSyntax` uit TAAK-025.
- Voorbeelden: `samples/SampleShop/SampleShop/Features/Orders/OrderPlacedHandler.cs`, `CancelOrderCommand.cs` (twee handlers, sync en async).

## Analyse

- Mediator 3.0.2: `INotificationHandler<T>.Handle` geeft `ValueTask`; `INotificationHandler\`1`staat al in`SwappedTypes`.

| Geval                                         | Herschrijving                           |
| --------------------------------------------- | --------------------------------------- |
| `async Task Handle` (blok of expression body) | alleen returntype `ValueTask`           |
| niet-async, alle returns `Task.CompletedTask` | `ValueTask` + `ValueTask.CompletedTask` |
| andere `Task`, expliciete implementatie       | Manual                                  |

## Keuzes

- **`ValueTask.CompletedTask`** boven `default`: leest als het origineel, .NET 5+.
- **Async expression body wel Fixed** (anders dan bij de void-handler): er hoeft geen `return` bij.
- Publisher-strategie (`TaskWhenAllPublisher`) hoort bij `mediatr.add-mediatr` (rij 5).

## Stappen

- [x] 1. Claim rij 3.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-notification-handler-plan.md` (approved).
- [x] 3. `NotificationHandlerPattern`, registratie in `MediatRFix`.
- [x] 4. Proofs `async`, `completed-task`, `completed-task-expression`, `two-handlers`; Manual `other-return`, `explicit`.
- [x] 5. Modellen, verify, README-rij, queue-rij.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 381/381, sample-tests 38/38.
- Scan SampleShop: `mediatr.notification-handler` 3 Fixed, 0 Manual. Volledige scan: zie TAAK-025.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-025-mediatr-void-request-handler.md`
- `docs/projectinfo/patterns/mediatr-notification-handler-plan.md`
