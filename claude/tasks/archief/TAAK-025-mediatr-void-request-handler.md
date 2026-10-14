# TAAK-025 — mediatr.void-request-handler

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `IRequestHandler<TReq>` with `Task Handle(...)` (MediatR 12.5.0) → `ValueTask<Unit> Handle(...)` (Mediator 3.0.2).

## Context for the next agent

- Row 2 in `claude/tasks/mediatr-queue.md`. Style and helpers of TAAK-023 (`mediatr.request-handler`).
- Example: `samples/SampleShop/SampleShop/Features/Orders/CancelOrderCommand.cs` (`CancelOrderHandler`, async).

## Analysis

- Mediator 3.0.2 (`~/.nuget/packages/mediator.abstractions/3.0.2/lib/net8.0/Mediator.dll`) has `IRequestHandler\`1`=`IRequestHandler<TRequest, Unit>`: `Handle`returns`ValueTask<Unit>`. `Unit`has`Value`and`ValueTask` (`get_ValueTask`in the assembly). Proven because the`after` proofs compile against it.
- `IRequest`, `IRequestHandler\`1`and`Unit`are already in`MediatRFix.SwappedTypes`; only `Handle` has to change.

| Case                                          | Rewrite                                                                                                  |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `async Task Handle` with a block              | `async ValueTask<Unit>`; `return;` → `return Unit.Value;`; reachable end → add `return Unit.Value;`      |
| non-async, all returns `Task.CompletedTask`   | `ValueTask<Unit>` + `Unit.ValueTask`                                                                     |
| `async` expression body                       | Manual                                                                                                   |
| other `Task` (e.g. `Task.Delay`), explicit    | Manual                                                                                                   |

## Choices

- **Helper `MediatRSyntax`** (`src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs`): finding (`FindHandleMethods`), `ToValueTask`, `ReturnedExpressions`, `IsTaskMember`, `OnValueTask` shared with `RequestHandlerPattern` and `NotificationHandlerPattern` (assignment: extract shared Task→ValueTask logic).
- **Reachable end via `AnalyzeControlFlow`**: correct with `if/else` and `throw` (proof `throw-at-end`), instead of looking at the last statement.
- **`Unit.ValueTask`** over `ValueTask.FromResult(Unit.Value)`: shorter, exists in Mediator 3.0.2.
- **Async expression body → Manual**: creating a block requires formatting guesses; rare.

## Steps

- [x] 1. Claim row 2.
- [x] 2. API in the assembly, example in samples.
- [x] 3. Plan `docs/projectinfo/patterns/mediatr-void-request-handler-plan.md` (approved).
- [x] 4. `VoidRequestHandlerPattern`, `MediatRSyntax`, registration in `MediatRFix`.
- [x] 5. Proofs `async`, `early-return`, `throw-at-end`, `completed-task`, `completed-task-expression`; Manual `other-return`, `async-expression`, `explicit`.
- [x] 6. Models, verify, README row, queue row.

## Result

- `dotnet build` 0 warnings, `dotnet test` 381/381, sample tests 38/38.
- Scan SampleShop (`--fix mediatr`, after restore): `mediatr.void-request-handler` 1 Fixed (`CancelOrderHandler`), `mediatr.notification-handler` 3, `mediatr.request-handler` 5, `project-file` 4 Fixed / 1 Manual (`SampleShop.csproj`: MediatR still used by `AddMediatR`, `IPipelineBehavior`, `IRequestExceptionHandler`: rows 4, 5, 7).

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-023-mediatr-request-handler.md`
- `claude/tasks/archief/TAAK-026-mediatr-notification-handler.md`
- `docs/projectinfo/patterns/mediatr-void-request-handler-plan.md`
