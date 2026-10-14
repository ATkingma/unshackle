# TAAK-029 — mediatr.send-as-task

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: MediatR `Send`/`Publish` whose result is used as a `Task` → `.AsTask()` (Mediator 3.0.2 returns `ValueTask`).

## Context for the next agent

- Row 6 in `claude/tasks/mediatr-queue.md`. Examples: `samples/SampleShop/SampleShop/Features/Orders/OrderFacade.cs`, `Func<Task> act = () => Sender.Send(...)` in the test projects.

## Analysis

- Mediator 3.0.2 (`Mediator.xml`): `ISender.Send<T>(IRequest<T>, ct)` → `ValueTask<T>`, `Send(object, ct)` → `ValueTask<object?>`, `IPublisher.Publish` → `ValueTask`, `CreateStream` → `IAsyncEnumerable<T>` (like MediatR).
- After the namespace swap `await sender.Send(...)` compiles unchanged; every other use (return as `Task`, `Func<Task>`, `Task.WhenAll`, `.ContinueWith`, `.Wait()`) does not, or behaves differently.

## Choices

- **Everything except a direct `await` gets `.AsTask()`** (via `GetSymbolInfo`: method `Send`/`Publish` from assembly `MediatR`). Simpler and safer than judging the converted type per use: `.AsTask()` gives exactly the old Task behaviour, also for `var t = ...` that is awaited more than once.
- **`await x.ConfigureAwait(...)` and `await (x)` stay as is**: `ValueTask` has `ConfigureAwait`.
- **Not `CreateStream`**: return type is the same.
- **Never Manual**.

## Steps

- [x] 1. Claim row 6.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-send-as-task-plan.md` (approved).
- [x] 3. `SendAsTaskPattern`, registration in `MediatRFix`.
- [x] 4. Proofs `task-uses`, `send-object`.
- [x] 5. Models, verify, README row, queue row.

## Result

- `dotnet build` 0 warnings, `dotnet test` 458/458, sample tests 38/38.
- Scan SampleShop: send-as-task 5/0.

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-037-mediatr-unsupported.md`
- `docs/projectinfo/patterns/mediatr-send-as-task-plan.md`
