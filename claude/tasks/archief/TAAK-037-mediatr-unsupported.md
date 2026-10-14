# TAAK-037 — mediatr.unsupported (+ RegisterGenericHandlers, stream swap, end-to-end)

**Status**: ✅ completed
**Created**: 2026-10-11
**Goal**: report MediatR constructs without a 1-to-1 counterpart in Mediator 3.0.2 as Manual with concrete advice; investigate what does have a twin; migrate SampleShop end-to-end.

## Context for the next agent

- Row 7 in `claude/tasks/mediatr-queue.md`. Examples: `samples/SampleShop/SampleShop/Behaviors/`, `Features/Common/CountQuery.cs`, `Features/Orders/CancelOrderCommand.cs`.

## Analysis (Mediator 3.0.2, `Mediator.dll`/`Mediator.xml`/README)

- **Does have a twin**: `IStreamRequest<T>`, `IStreamRequestHandler<T,R>` (the same `IAsyncEnumerable<R> Handle(request, ct)`), `ISender.Send(object)`/`IPublisher.Publish(object)` (→ `ValueTask`), `TaskWhenAllPublisher`/`ForeachAwaitPublisher`.
- **Different shape**: `MessagePreProcessor<TMessage,TResponse>`, `MessagePostProcessor<,>`, `MessageExceptionHandler<,,>` (abstract classes, registered as pipeline behaviors); `INotificationPublisher.Publish(NotificationHandlers<T>, T, ct)`; `IStreamPipelineBehavior<TMessage,TResponse>` with `StreamHandlerDelegate<TMessage,TResponse>`.
- **None**: generic requests/notifications and generic request handlers (README "doesn't support generic requests/notifications"; generator: "request/query/command handlers cannot be generic"). On the SampleShop copy `CountQuery<TEntity>` gave CS0246 in `Mediator.g.cs`. Mediator registers open generic _notification_ handlers itself.

## Choices

- **`IStreamRequest`1` and `IStreamRequestHandler`2` to `MediatRFix.SwappedTypes`** instead of Manual; proven with `SwappedTypesTests.FixAsync_StreamRequestHandler_CompilesAgainstMediator` (compiles with the generator). No own pattern: there is nothing to rewrite.
- **`Send(object)` no Manual**: it exists; `mediatr.send-as-task` covers Task usage.
- **`UnsupportedPattern` reports per type declaration** (pre/post processor, exception handler/action, notification publisher, stream behavior, generic message or request handler) with advice in the reason.
- **`RegisterGenericHandlers` is dropped in `AddMediatRPattern`** (proof `AddMediatR/generic-handlers`): the flag has no meaning in Mediator; the real problem (a generic request handler) is reported by `mediatr.unsupported` at the place itself.
- **`NotificationPublisher = new TaskWhenAllPublisher()` stays Manual** (see TAAK-060).

## Steps

- [x] 1. Claim row 7.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-unsupported-plan.md` (approved).
- [x] 3. `UnsupportedPattern`, `SwappedTypes`, `RegisterGenericHandlers` in `AddMediatRPattern`.
- [x] 4. Proofs `Unsupported/*` (6 Manual), `AddMediatR/generic-handlers`; stream swap test.
- [x] 5. Models, verify, README row, add-mediatr docs updated, queue row.
- [x] 6. Scan and end-to-end on a copy of SampleShop.

## Result

- `dotnet build` 0 warnings, `dotnet test` 458/458, sample tests 38/38.
- Scan SampleShop (`--fix mediatr`): add-mediatr 0/1 (`NotificationPublisher`), notification-handler 3/0, pipeline-behavior 2/0, request-handler 5/0, send-as-task 5/0, unsupported 0/6, void-request-handler 1/0, project-file 4/1. Total 20 fixed, 8 manual.
- **End-to-end** (`fix` on a copy in the scratchpad): does **not** build directly (CS0104: `TaskWhenAllPublisher` ambiguous between `Mediator` and `MediatR.NotificationPublishers`, because the using swap still puts `using Mediator;` in a file with a Manual). After finishing the 8 Manuals by hand following the advice (AddMediator with `NotificationPublisherType`, `PipelineBehaviors` incl. pre/post/exception, `StreamPipelineBehaviors`; processors as `MessagePre/PostProcessor`; exception handler as `MessageExceptionHandler` with `Handled(Unit.Value)`; `CountQuery<T>` split; `MediatR` package removed) **the copy builds (0 errors) and 38/38 tests pass**. CS0436 warnings in both test projects though (see TAAK-063).
- Follow-up tasks: TAAK-060 (publisher + ambiguous using), TAAK-061 (rewrite pre/post/exception handlers), TAAK-062 (rewrite stream behavior), TAAK-063 (source generator only in the host project).

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-028-mediatr-add-mediatr.md`
- `claude/tasks/archief/TAAK-029-mediatr-send-as-task.md`
- `docs/projectinfo/patterns/mediatr-unsupported-plan.md`
