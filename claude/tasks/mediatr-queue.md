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
| 6   | `mediatr.send-as-task`         | `ISender`/`IMediator`/`IPublisher` `.Send/.Publish` waar het resultaat als `Task` gebruikt wordt (bv. `Func<Task> act = () => sender.Send(...)`) | `.Send(...).AsTask()`; met `await` direct niets te doen                                                                                                     | klaar (TAAK-029) |
| 7   | `mediatr.unsupported`          | `IStreamRequest`-handlers, `IRequestPreProcessor`/`PostProcessor`, `IRequestExceptionHandler`, `Send(object)`                                    | Manual met reden (rapport)                                                                                                                                  | klaar (TAAK-037) |

### Uit het corpus (`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`, vormen in ≥2 projecten)

| #   | Patroon-ID / bug                                   | MediatR 12.x                                                                                                                | Mediator (verwacht)                                                                      | Status           |
| --- | -------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | ---------------- |
| 8   | `mediatr.behavior-di-registration`                 | `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` buiten `AddMediatR` (jason, ardalis)                    | Mediator 3 leest behaviors ook uit DI: alleen namespace-swap; eerst meten, anders Manual | klaar (TAAK-145) |
| 9   | bug: handler via eigen marker-interface            | `ICommandHandler<T> : IRequestHandler<T>`, `IIntegrationEventHandler<T> : INotificationHandler<T>` (kgrzybek, evolutionary) | `ValueTask Handle` zoals bij directe implementatie                                       | klaar (TAAK-080) |
| 11  | `mediatr.request-processor`                        | `IRequestPreProcessor`/`IRequestPostProcessor`                                                                              | `MessagePreProcessor`/`MessagePostProcessor`                                             | klaar (TAAK-061) |
| 12  | `mediatr.exception-handler`                        | `IRequestExceptionHandler` + `state.SetHandled`                                                                             | `MessageExceptionHandler` + `Handled(x)`/`NotHandled`                                    | klaar (TAAK-061) |
| 13  | `mediatr.stream-pipeline-behavior`                 | `IStreamPipelineBehavior`, `next()`                                                                                         | `next(request, ct)`, `IStreamMessage`                                                    | klaar (TAAK-062) |
| 10  | bug: behavior met `IRequest<TResponse>`-constraint | `where TRequest : IRequest<TResponse>` (eShop, ardalis)                                                                     | constraint blijft, geen extra `, IMessage`                                               | klaar (TAAK-081) |

Interfaces met dezelfde naam (`IRequest<T>`, `INotification`, `ISender`, `IPublisher`, `IMediator`, `Unit`) krijgen geen eigen patroon:
de namespace-swap van de fix (`MediatR` → `Mediator`) dekt ze via `MediatRFix.SwappedTypes` (TAAK-024).

Vervolg na de end-to-end-run (TAAK-037): TAAK-060 (NotificationPublisher, dubbelzinnige using, klaar), TAAK-061 (pre/post/exception-handlers, klaar), TAAK-062 (stream-behavior, klaar), TAAK-063 (source generator alleen in host-project, klaar).

Corpus-bugs (2026-10-11): TAAK-130 (findings op originele semantiek, bestandsvolgorde) en TAAK-134 (projectbestanden tekstueel splicen) klaar; evo1/evo2 lopen nu door de fix heen (evo1: alleen nog MSG0005 TAAK-136 + IDE0005/IDE0065-usings; evo2: alleen nog TAAK-131).
TAAK-132 (abstracte generieke basis geen Manual), TAAK-133 (pakket dat op MediatR bouwt, bv. Ardalis.SharedKernel → Manual; ardalis blijft heel) en TAAK-136 (`WarningsNotAsErrors` MSG0005 naast `Mediator.SourceGenerator`) klaar. evo1 na fix: MSG0005 nu warning; resteert IDE0005/IDE0065 (usings buiten namespace) en IDE0060 (`assembly`-parameter ongebruikt na `AddMediatR` → `AddMediator`, nog geen taak).

Alles-of-niets (2026-10-11): `MediatRFix.AllOrNothing`; één Manual-bevinding laat de hele solution ongemoeid (half gemigreerd bouwde wel maar faalde runtime).

### Corpus 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)

| #   | Taak     | Wat                                                                                         | Project        | Status |
| --- | -------- | ------------------------------------------------------------------------------------------- | -------------- | ------ |
| 14  | TAAK-180 | `RequestHandlerDelegate` in testcode (mock + `Handle(cmd, next, ct)`); stil "still used"    | amantin        | open   |
| 15  | TAAK-181 | zelfsluitend `<Project Sdk="..." />` blokkeert projectbestand-stap op alle projecten        | kgrzybek       | open   |
| 16  | TAAK-182 | inactieve `#if`-code zonder MediatR houdt `global using MediatR` vast                        | jason          | open   |
| 17  | TAAK-183 | `AddMediatR` over projectgrenzen (internal handlers, assembly-parameter, testproject)       | ardalis, evo1/2 | open  |
| 18  | TAAK-184 | request-processor `return Task.Run(...)` → `new ValueTask(...)`                             | nadirbad       | open   |

## Zie ook

- `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`
- `claude/tasks/archief/TAAK-024-gelijknamige-typen-swap.md` — `SwappedTypes`, `MediatR.Contracts` weg
- `claude/tasks/patroon-queue.md` — de FA-wachtrij (voorbeeld)
- `claude/docs/patroon-loop.md`
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — frequenties uit open-source projecten
- `claude/tasks/archief/TAAK-080-mediatr-marker-interface-handler.md`, `claude/tasks/archief/TAAK-081-mediatr-behavior-request-constraint.md`
