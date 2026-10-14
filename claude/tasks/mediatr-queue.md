# Pattern queue (MediatR 12.5.0 → Mediator 3.x)

Work list for the pattern loop (`claude/docs/patroon-loop.md`), same rules as `claude/tasks/patroon-queue.md`.
When claimed, each row gets the next free TAAK number and a task according to `/taak-specifiek`
(`.claude/skills/taak-specifiek/SKILL.md`). Row 0 is `TAAK-020`; all patterns wait for it.
Examples: `samples/SampleShop/SampleShop/` and `SampleShop.Tests/OrderHandlerTests.cs`.
Code under `src/Unshackle.Core/Fixes/MediatR/Patterns/`, proofs under `src/Unshackle.Tests/Proofs/MediatR/<Name>/`.

Statuses: `open` · `in progress (TAAK-NNN)` · `blocked (TAAK-NNN, waiting for TAAK-MMM)` · `done (TAAK-NNN)` · `failed (TAAK-NNN, attempt N)`

| #   | Pattern ID                     | MediatR 12.5.0                                                                                                                                   | Mediator (expected)                                                                                                                                         | Status           |
| --- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------- |
| 0   | _infrastructure_               | `MediatRFix`, multiple packages, proof runner per fix                                                                                            | —                                                                                                                                                           | done (TAAK-020)  |
| 1   | `mediatr.request-handler`      | `IRequestHandler<TReq,TRes>` with `Task<TRes> Handle(...)`                                                                                       | `ValueTask<TRes> Handle(...)`; `Task.FromResult(x)` → `ValueTask.FromResult(x)` / `new(x)`, `async` stays                                                   | done (TAAK-023)  |
| 2   | `mediatr.void-request-handler` | `IRequest` + `IRequestHandler<TReq>` with `Task Handle(...)`                                                                                     | `IRequest` → `IRequest<Unit>` semantics: `ValueTask<Unit> Handle`, `return Unit.Value`                                                                      | done (TAAK-025)  |
| 3   | `mediatr.notification-handler` | `INotificationHandler<T>` with `Task Handle(...)`                                                                                                | `ValueTask Handle(...)`; `Task.CompletedTask` → `ValueTask.CompletedTask` / `default`                                                                       | done (TAAK-026)  |
| 4   | `mediatr.pipeline-behavior`    | `IPipelineBehavior<TReq,TRes>`, `RequestHandlerDelegate<T> next`, `next()`                                                                       | `IPipelineBehavior<TMsg,TRes>` with `where TMsg : IMessage`, `MessageHandlerDelegate<TMsg,TRes> next`, `next(message, ct)`, `ValueTask<TRes>`; otherwise Manual | done (TAAK-027)  |
| 5   | `mediatr.add-mediatr`          | `services.AddMediatR(cfg => { RegisterServicesFrom...; AddOpenBehavior(...) })`                                                                  | `services.AddMediator(o => { o.ServiceLifetime = ...; o.PipelineBehaviors = [typeof(...)] })`; unknown `cfg` calls → Manual                                 | done (TAAK-028)  |
| 6   | `mediatr.send-as-task`         | `ISender`/`IMediator`/`IPublisher` `.Send/.Publish` where the result is used as a `Task` (e.g. `Func<Task> act = () => sender.Send(...)`)         | `.Send(...).AsTask()`; with `await` directly nothing to do                                                                                                  | done (TAAK-029)  |
| 7   | `mediatr.unsupported`          | `IStreamRequest` handlers, `IRequestPreProcessor`/`PostProcessor`, `IRequestExceptionHandler`, `Send(object)`                                    | Manual with reason (report)                                                                                                                                 | done (TAAK-037)  |

### From the corpus (`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`, shapes in ≥2 projects)

| #   | Pattern ID / bug                                   | MediatR 12.x                                                                                                                | Mediator (expected)                                                                      | Status           |
| --- | -------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | ---------------- |
| 8   | `mediatr.behavior-di-registration`                 | `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` outside `AddMediatR` (jason, ardalis)                   | Mediator 3 also reads behaviors from DI: namespace swap only; measure first, otherwise Manual | done (TAAK-145) |
| 9   | bug: handler via own marker interface              | `ICommandHandler<T> : IRequestHandler<T>`, `IIntegrationEventHandler<T> : INotificationHandler<T>` (kgrzybek, evolutionary) | `ValueTask Handle` as with a direct implementation                                       | done (TAAK-080)  |
| 11  | `mediatr.request-processor`                        | `IRequestPreProcessor`/`IRequestPostProcessor`                                                                              | `MessagePreProcessor`/`MessagePostProcessor`                                             | done (TAAK-061)  |
| 12  | `mediatr.exception-handler`                        | `IRequestExceptionHandler` + `state.SetHandled`                                                                             | `MessageExceptionHandler` + `Handled(x)`/`NotHandled`                                    | done (TAAK-061)  |
| 13  | `mediatr.stream-pipeline-behavior`                 | `IStreamPipelineBehavior`, `next()`                                                                                         | `next(request, ct)`, `IStreamMessage`                                                    | done (TAAK-062)  |
| 10  | bug: behavior with `IRequest<TResponse>` constraint | `where TRequest : IRequest<TResponse>` (eShop, ardalis)                                                                    | constraint stays, no extra `, IMessage`                                                  | done (TAAK-081)  |

Interfaces with the same name (`IRequest<T>`, `INotification`, `ISender`, `IPublisher`, `IMediator`, `Unit`) do not get their own pattern:
the fix's namespace swap (`MediatR` → `Mediator`) covers them via `MediatRFix.SwappedTypes` (TAAK-024).

Follow-up after the end-to-end run (TAAK-037): TAAK-060 (NotificationPublisher, ambiguous using, done), TAAK-061 (pre/post/exception handlers, done), TAAK-062 (stream behavior, done), TAAK-063 (source generator only in host project, done).

Corpus bugs (2026-10-11): TAAK-130 (findings on original semantics, file order) and TAAK-134 (splice project files textually) done; evo1/evo2 now pass through the fix (evo1: only MSG0005 TAAK-136 + IDE0005/IDE0065 usings left; evo2: only TAAK-131 left).
TAAK-132 (abstract generic base not Manual), TAAK-133 (package built on MediatR, e.g. Ardalis.SharedKernel → Manual; ardalis stays intact) and TAAK-136 (`WarningsNotAsErrors` MSG0005 alongside `Mediator.SourceGenerator`) done. evo1 after fix: MSG0005 now a warning; remaining are IDE0005/IDE0065 (usings outside namespace) and IDE0060 (`assembly` parameter unused after `AddMediatR` → `AddMediator`, no task yet).

All-or-nothing (2026-10-11): `MediatRFix.AllOrNothing`; a single Manual finding leaves the whole solution untouched (half-migrated did build but failed at runtime).

### Corpus 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)

| #   | Task     | What                                                                                        | Project        | Status |
| --- | -------- | ------------------------------------------------------------------------------------------- | -------------- | ------ |
| 14  | TAAK-180 | `RequestHandlerDelegate` in test code (mock + `Handle(cmd, next, ct)`); silently "still used" | amantin      | done (TAAK-180) |
| 15  | TAAK-181 | self-closing `<Project Sdk="..." />` blocks the project file step on all projects           | kgrzybek       | done   |
| 16  | TAAK-182 | inactive `#if` code without MediatR keeps `global using MediatR` alive                      | jason          | done   |
| 17  | TAAK-183 | `AddMediatR` across project boundaries (internal handlers, assembly parameter, test project) | ardalis, evo1/2 | done (TAAK-183) |
| 18  | TAAK-184 | request processor `return Task.Run(...)` → `new ValueTask(...)`                             | nadirbad       | done   |
| 19  | TAAK-190 | callers of a rewritten request processor (`new P<Cmd>()`, `.Process(...)`) silently break the build | jason | done (TAAK-190) |
| 20  | TAAK-191 | one `RequestHandlerDelegate` mock shared by multiple request types                          | amantin        | done (TAAK-191) |

### 100% round

| #   | Task     | What                                                                                        | Project        | Status |
| --- | -------- | ------------------------------------------------------------------------------------------- | -------------- | ------ |
| 22  | TAAK-192 | generic notification `DomainEventNotification<T>` + handlers per closed type                | nadirbad       | done (TAAK-192) |
| 23  | TAAK-193 | generic decorators/handlers/messages (`*CommandHandlerDecorator<T>`, `DomainNotificationBase<T>`, `IdentifiedCommand<T,R>`) | kgrzybek, eShop | done (TAAK-193): `mediatr.generic-decorator`, `mediatr.generic-request`; corpus repetition open |
| 24  | TAAK-194 | external package on MediatR (Ardalis.SharedKernel): vendor source code locally, replace package | ardalis    | done (TAAK-194): vendored Ardalis.SharedKernel |
| 25  | TAAK-195 | `AddMediatR` across project boundaries: internal handlers, `Assembly` parameter, test project | ardalis, evo1/2 | failed (TAAK-195, attempt 1): internal handlers done (`mediatr.internal-handler-access`, ardalis 0 Manual); evo1 test project and evo2 `Assembly` parameter have no exact translation |
| 26  | TAAK-196 | manual Autofac registration of MediatR (`MediatorModule`, 30× unmatched-use on `MediatR.Pipeline`) | kgrzybek | done (TAAK-196): `mediatr.autofac-mediator-module` |
| 27  | TAAK-197 | own `IMediator` implementation in test code (ardalis `NoOpMediator`, `Task` signatures) breaks the build | ardalis | done (9189df4) |

### Corpus round 3 (2026-10-13)

| #   | Task     | What                                                                                        | Project        | Status |
| --- | -------- | ------------------------------------------------------------------------------------------- | -------------- | ------ |
| 28  | TAAK-210 | generic decorator via own marker, file without `using MediatR` → still `mediatr.unsupported` | kgrzybek | done (TAAK-210): 0 Manual; also non-CPM `Directory.Packages.props`, `using Mediator` on void handlers, SA1513; build → #30 |
| 29  | TAAK-211 | `mediatr.send-as-task` on NSubstitute setup → `.AsTask().Returns(...)` (NS1000, silent build break) | dotnet/eShop | done (TAAK-211): eShop builds, unit 32/32 equal |
| 30  | TAAK-212 | kgrzybek does not build after the fix: MSG0001 (duplicate handlers), SA1210, CS1503, CS0122 | kgrzybek | done (TAAK-212): four error kinds gone; next layer → #31 |
| 31  | TAAK-213 | generator host references another host: CS0436/CS0121 in Registrations.Infrastructure | kgrzybek | done (TAAK-213): build green; archtests → #32 |
| 32  | TAAK-214 | generated Mediator types break archtests (module dependency), 4 regressions | kgrzybek | done (TAAK-214): legitimate difference (generated dispatcher in archtest), documented |
| 33  | TAAK-240 | report warning for NetArchTest/ArchUnitNET: exclude namespace `Mediator` | kgrzybek | done (TAAK-240): warning on AddMediatR/Autofac host |

## See also

- `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`
- `claude/tasks/archief/TAAK-024-gelijknamige-typen-swap.md` — `SwappedTypes`, `MediatR.Contracts` removed
- `claude/tasks/patroon-queue.md` — the FA queue (example)
- `claude/docs/patroon-loop.md`
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — frequencies from open-source projects
- `claude/tasks/archief/TAAK-080-mediatr-marker-interface-handler.md`, `claude/tasks/archief/TAAK-081-mediatr-behavior-request-constraint.md`
