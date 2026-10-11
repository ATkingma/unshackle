# Patroon-wachtrij (MediatR 12.5.0 → Mediator 3.x)

Werklijst voor de patroon-loop (`claude/docs/patroon-loop.md`), zelfde regels als `claude/tasks/patroon-queue.md`.
Elke rij krijgt bij het claimen het volgende vrije TAAK-nummer en een taak volgens `/taak-specifiek`
(`.claude/skills/taak-specifiek/SKILL.md`). Rij 0 is `TAAK-020`; alle patronen wachten daarop.
Voorbeelden: `samples/SampleShop/SampleShop/` en `SampleShop.Tests/OrderHandlerTests.cs`.
Code onder `src/Unshackle.Core/Fixes/MediatR/Patterns/`, proofs onder `src/Unshackle.Tests/Proofs/MediatR/<Naam>/`.

Statussen: `open` · `bezig (TAAK-NNN)` · `geblokkeerd (TAAK-NNN, wacht op TAAK-MMM)` · `klaar (TAAK-NNN)` · `mislukt (TAAK-NNN, poging N)`

| #   | Patroon-ID                     | MediatR 12.5.0                                                                                                                                   | Mediator (verwacht)                                                                                                                                         | Status           |
| --- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------- |
| 0   | _infrastructuur_               | `MediatRFix`, meerdere pakketten, proof-runner per fix                                                                                           | —                                                                                                                                                           | klaar (TAAK-020) |
| 1   | `mediatr.request-handler`      | `IRequestHandler<TReq,TRes>` met `Task<TRes> Handle(...)`                                                                                        | `ValueTask<TRes> Handle(...)`; `Task.FromResult(x)` → `ValueTask.FromResult(x)` / `new(x)`, `async` blijft                                                  | klaar (TAAK-023) |
| 2   | `mediatr.void-request-handler` | `IRequest` + `IRequestHandler<TReq>` met `Task Handle(...)`                                                                                      | `IRequest` → `IRequest<Unit>`-semantiek: `ValueTask<Unit> Handle`, `return Unit.Value`                                                                      | klaar (TAAK-025) |
| 3   | `mediatr.notification-handler` | `INotificationHandler<T>` met `Task Handle(...)`                                                                                                 | `ValueTask Handle(...)`; `Task.CompletedTask` → `ValueTask.CompletedTask` / `default`                                                                       | klaar (TAAK-026) |
| 4   | `mediatr.pipeline-behavior`    | `IPipelineBehavior<TReq,TRes>`, `RequestHandlerDelegate<T> next`, `next()`                                                                       | `IPipelineBehavior<TMsg,TRes>` met `where TMsg : IMessage`, `MessageHandlerDelegate<TMsg,TRes> next`, `next(message, ct)`, `ValueTask<TRes>`; anders Manual | klaar (TAAK-027) |
| 5   | `mediatr.add-mediatr`          | `services.AddMediatR(cfg => { RegisterServicesFrom...; AddOpenBehavior(...) })`                                                                  | `services.AddMediator(o => { o.ServiceLifetime = ...; o.PipelineBehaviors = [typeof(...)] })`; onbekende `cfg`-aanroepen → Manual                           | klaar (TAAK-028) |
| 6 | `mediatr.send-as-task` | `ISender`/`IMediator`/`IPublisher` `.Send/.Publish` waar het resultaat als `Task` gebruikt wordt (bv. `Func<Task> act = () => sender.Send(...)`) | `.Send(...).AsTask()`; met `await` direct niets te doen | klaar (TAAK-029) |
| 7 | `mediatr.unsupported` | `IStreamRequest`-handlers, `IRequestPreProcessor`/`PostProcessor`, `IRequestExceptionHandler`, `Send(object)` | Manual met reden (rapport) | klaar (TAAK-037) |

### Uit het corpus (`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`, vormen in ≥2 projecten)

| # | Patroon-ID / bug | MediatR 12.x | Mediator (verwacht) | Status |
| --- | -------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | --------------- |
| 8 | `mediatr.behavior-di-registration` | `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` buiten `AddMediatR` (jason, ardalis) | Mediator 3 leest behaviors ook uit DI: alleen namespace-swap; eerst meten, anders Manual | open |
| 9 | bug: handler via eigen marker-interface | `ICommandHandler<T> : IRequestHandler<T>`, `IIntegrationEventHandler<T> : INotificationHandler<T>` (kgrzybek, evolutionary) | `ValueTask Handle` zoals bij directe implementatie | klaar (TAAK-080) |
| 10 | bug: behavior met `IRequest<TResponse>`-constraint | `where TRequest : IRequest<TResponse>` (eShop, ardalis) | constraint blijft, geen extra `, IMessage` | klaar (TAAK-081) |

Interfaces met dezelfde naam (`IRequest<T>`, `INotification`, `ISender`, `IPublisher`, `IMediator`, `Unit`) krijgen geen eigen patroon:
de namespace-swap van de fix (`MediatR` → `Mediator`) dekt ze via `MediatRFix.SwappedTypes` (TAAK-024).

Vervolg na de end-to-end-run (TAAK-037): TAAK-060 (NotificationPublisher, dubbelzinnige using, klaar), TAAK-061 (pre/post/exception-handlers, open), TAAK-062 (stream-behavior, open), TAAK-063 (source generator alleen in host-project, klaar).

## Zie ook

- `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`
- `claude/tasks/archief/TAAK-024-gelijknamige-typen-swap.md` — `SwappedTypes`, `MediatR.Contracts` weg
- `claude/tasks/patroon-queue.md` — de FA-wachtrij (voorbeeld)
- `claude/docs/patroon-loop.md`
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — frequenties uit open-source projecten
- `claude/tasks/archief/TAAK-080-mediatr-marker-interface-handler.md`, `claude/tasks/archief/TAAK-081-mediatr-behavior-request-constraint.md`
