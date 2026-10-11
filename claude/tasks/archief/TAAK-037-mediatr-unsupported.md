# TAAK-037 — mediatr.unsupported (+ RegisterGenericHandlers, stream-swap, end-to-end)

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: MediatR-constructies zonder 1-op-1-tegenhanger in Mediator 3.0.2 als Manual melden met concreet advies; onderzoeken wat wél een tweeling heeft; SampleShop end-to-end migreren.

## Context voor de volgende agent

- Rij 7 in `claude/tasks/mediatr-queue.md`. Voorbeelden: `samples/SampleShop/SampleShop/Behaviors/`, `Features/Common/CountQuery.cs`, `Features/Orders/CancelOrderCommand.cs`.

## Analyse (Mediator 3.0.2, `Mediator.dll`/`Mediator.xml`/README)

- **Wel een tweeling**: `IStreamRequest<T>`, `IStreamRequestHandler<T,R>` (zelfde `IAsyncEnumerable<R> Handle(request, ct)`), `ISender.Send(object)`/`IPublisher.Publish(object)` (→ `ValueTask`), `TaskWhenAllPublisher`/`ForeachAwaitPublisher`.
- **Andere vorm**: `MessagePreProcessor<TMessage,TResponse>`, `MessagePostProcessor<,>`, `MessageExceptionHandler<,,>` (abstracte klassen, als pipeline-behavior geregistreerd); `INotificationPublisher.Publish(NotificationHandlers<T>, T, ct)`; `IStreamPipelineBehavior<TMessage,TResponse>` met `StreamHandlerDelegate<TMessage,TResponse>`.
- **Geen**: generieke requests/notifications en generieke request-handlers (README "doesn't support generic requests/notifications"; generator: "request/query/command handlers cannot be generic"). Op de SampleShop-kopie gaf `CountQuery<TEntity>` CS0246 in `Mediator.g.cs`. Open generieke _notification_-handlers registreert Mediator zelf.

## Keuzes

- **`IStreamRequest`1`en`IStreamRequestHandler`2` naar `MediatRFix.SwappedTypes`** in plaats van Manual; bewezen met `SwappedTypesTests.FixAsync_StreamRequestHandler_CompilesAgainstMediator` (compileert met de generator). Geen eigen pattern: er valt niets te herschrijven.
- **`Send(object)` geen Manual**: bestaat; `mediatr.send-as-task` dekt Task-gebruik.
- **`UnsupportedPattern` meldt per typedeclaratie** (pre/post-processor, exception-handler/-action, notification-publisher, stream-behavior, generiek bericht of request-handler) met advies in de reden.
- **`RegisterGenericHandlers` valt weg in `AddMediatRPattern`** (proof `AddMediatR/generic-handlers`): de vlag heeft in Mediator geen betekenis; het echte probleem (generieke request-handler) meldt `mediatr.unsupported` op de plek zelf.
- **`NotificationPublisher = new TaskWhenAllPublisher()` blijft Manual** (zie TAAK-060).

## Stappen

- [x] 1. Claim rij 7.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-unsupported-plan.md` (approved).
- [x] 3. `UnsupportedPattern`, `SwappedTypes`, `RegisterGenericHandlers` in `AddMediatRPattern`.
- [x] 4. Proofs `Unsupported/*` (6 Manual), `AddMediatR/generic-handlers`; test stream-swap.
- [x] 5. Modellen, verify, README-rij, add-mediatr-docs bijgewerkt, queue-rij.
- [x] 6. Scan en end-to-end op een kopie van SampleShop.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 458/458, sample-tests 38/38.
- Scan SampleShop (`--fix mediatr`): add-mediatr 0/1 (`NotificationPublisher`), notification-handler 3/0, pipeline-behavior 2/0, request-handler 5/0, send-as-task 5/0, unsupported 0/6, void-request-handler 1/0, project-file 4/1. Totaal 20 fixed, 8 manual.
- **End-to-end** (`fix` op een kopie in de scratchpad): bouwt **niet** direct (CS0104: `TaskWhenAllPublisher` dubbelzinnig tussen `Mediator` en `MediatR.NotificationPublishers`, omdat de using-swap in een bestand met een Manual toch `using Mediator;` zet). Na het met de hand afwerken van de 8 Manuals volgens de adviezen (AddMediator met `NotificationPublisherType`, `PipelineBehaviors` incl. pre/post/exception, `StreamPipelineBehaviors`; processors als `MessagePre/PostProcessor`; exception-handler als `MessageExceptionHandler` met `Handled(Unit.Value)`; `CountQuery<T>` gesplitst; `MediatR`-pakket weg) **bouwt de kopie (0 errors) en slagen 38/38 tests**. Wel CS0436-warnings in beide testprojecten (zie TAAK-063).
- Vervolgtaken: TAAK-060 (publisher + dubbelzinnige using), TAAK-061 (pre/post/exception-handlers herschrijven), TAAK-062 (stream-behavior herschrijven), TAAK-063 (source generator alleen in het host-project).

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-028-mediatr-add-mediatr.md`
- `claude/tasks/archief/TAAK-029-mediatr-send-as-task.md`
- `docs/projectinfo/patterns/mediatr-unsupported-plan.md`
