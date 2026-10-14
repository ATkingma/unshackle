# TAAK-026 — mediatr.notification-handler

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `INotificationHandler<T>` with `Task Handle(...)` (MediatR 12.5.0) → `ValueTask Handle(...)` (Mediator 3.0.2).

## Context for the next agent

- Row 3 in `claude/tasks/mediatr-queue.md`. Reuses `MediatRSyntax` from TAAK-025.
- Examples: `samples/SampleShop/SampleShop/Features/Orders/OrderPlacedHandler.cs`, `CancelOrderCommand.cs` (two handlers, sync and async).

## Analysis

- Mediator 3.0.2: `INotificationHandler<T>.Handle` returns `ValueTask`; `INotificationHandler\`1`is already in`SwappedTypes`.

| Case                                          | Rewrite                                 |
| --------------------------------------------- | --------------------------------------- |
| `async Task Handle` (block or expression body) | return type `ValueTask` only           |
| non-async, all returns `Task.CompletedTask`   | `ValueTask` + `ValueTask.CompletedTask` |
| other `Task`, explicit implementation         | Manual                                  |

## Choices

- **`ValueTask.CompletedTask`** over `default`: reads like the original, .NET 5+.
- **Async expression body is Fixed** (unlike the void handler): no `return` needs to be added.
- Publisher strategy (`TaskWhenAllPublisher`) belongs to `mediatr.add-mediatr` (row 5).

## Steps

- [x] 1. Claim row 3.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-notification-handler-plan.md` (approved).
- [x] 3. `NotificationHandlerPattern`, registration in `MediatRFix`.
- [x] 4. Proofs `async`, `completed-task`, `completed-task-expression`, `two-handlers`; Manual `other-return`, `explicit`.
- [x] 5. Models, verify, README row, queue row.

## Result

- `dotnet build` 0 warnings, `dotnet test` 381/381, sample tests 38/38.
- Scan SampleShop: `mediatr.notification-handler` 3 Fixed, 0 Manual. Full scan: see TAAK-025.

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-025-mediatr-void-request-handler.md`
- `docs/projectinfo/patterns/mediatr-notification-handler-plan.md`
