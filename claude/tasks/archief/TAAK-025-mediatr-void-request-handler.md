# TAAK-025 — mediatr.void-request-handler

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `IRequestHandler<TReq>` met `Task Handle(...)` (MediatR 12.5.0) → `ValueTask<Unit> Handle(...)` (Mediator 3.0.2).

## Context voor de volgende agent

- Rij 2 in `claude/tasks/mediatr-queue.md`. Stijl en helpers van TAAK-023 (`mediatr.request-handler`).
- Voorbeeld: `samples/SampleShop/SampleShop/Features/Orders/CancelOrderCommand.cs` (`CancelOrderHandler`, async).

## Analyse

- Mediator 3.0.2 (`~/.nuget/packages/mediator.abstractions/3.0.2/lib/net8.0/Mediator.dll`) heeft `IRequestHandler\`1`=`IRequestHandler<TRequest, Unit>`: `Handle`geeft`ValueTask<Unit>`. `Unit`heeft`Value`en`ValueTask` (`get_ValueTask`in de assembly). Bewezen doordat de`after`-proofs ertegen compileren.
- `IRequest`, `IRequestHandler\`1`en`Unit`staan al in`MediatRFix.SwappedTypes`; alleen `Handle` moet mee.

| Geval                                         | Herschrijving                                                                                            |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `async Task Handle` met blok                  | `async ValueTask<Unit>`; `return;` → `return Unit.Value;`; bereikbaar einde → `return Unit.Value;` erbij |
| niet-async, alle returns `Task.CompletedTask` | `ValueTask<Unit>` + `Unit.ValueTask`                                                                     |
| `async` expression body                       | Manual                                                                                                   |
| andere `Task` (bv. `Task.Delay`), expliciet   | Manual                                                                                                   |

## Keuzes

- **Helper `MediatRSyntax`** (`src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs`): vinden (`FindHandleMethods`), `ToValueTask`, `ReturnedExpressions`, `IsTaskMember`, `OnValueTask` gedeeld met `RequestHandlerPattern` en `NotificationHandlerPattern` (opdracht: gedeelde Task→ValueTask-logica extraheren).
- **Bereikbaar einde via `AnalyzeControlFlow`**: correct bij `if/else` en `throw` (proof `throw-at-end`), in plaats van te kijken naar het laatste statement.
- **`Unit.ValueTask`** boven `ValueTask.FromResult(Unit.Value)`: korter, bestaat in Mediator 3.0.2.
- **Async expression body → Manual**: blok maken vraagt opmaakgokken; zeldzaam.

## Stappen

- [x] 1. Claim rij 2.
- [x] 2. API in de assembly, voorbeeld in samples.
- [x] 3. Plan `docs/projectinfo/patterns/mediatr-void-request-handler-plan.md` (approved).
- [x] 4. `VoidRequestHandlerPattern`, `MediatRSyntax`, registratie in `MediatRFix`.
- [x] 5. Proofs `async`, `early-return`, `throw-at-end`, `completed-task`, `completed-task-expression`; Manual `other-return`, `async-expression`, `explicit`.
- [x] 6. Modellen, verify, README-rij, queue-rij.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 381/381, sample-tests 38/38.
- Scan SampleShop (`--fix mediatr`, na restore): `mediatr.void-request-handler` 1 Fixed (`CancelOrderHandler`), `mediatr.notification-handler` 3, `mediatr.request-handler` 5, `project-file` 4 Fixed / 1 Manual (`SampleShop.csproj`: MediatR nog gebruikt door `AddMediatR`, `IPipelineBehavior`, `IRequestExceptionHandler`: rijen 4, 5, 7).

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-023-mediatr-request-handler.md`
- `claude/tasks/archief/TAAK-026-mediatr-notification-handler.md`
- `docs/projectinfo/patterns/mediatr-void-request-handler-plan.md`
