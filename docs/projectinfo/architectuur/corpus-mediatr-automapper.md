# Corpus: MediatR- en AutoMapper-gebruik in open-source projecten

Gemeten op 2026-10-11 met `grep -P` over `*.cs` (tarballs, alleen gelezen; niets gekopieerd).
Getallen zijn regex-treffers, geen Roslyn-analyse: richtinggevend, niet exact.

## Corpus

| Project (ref)                                 | MediatR                    | AutoMapper |
| --------------------------------------------- | -------------------------- | ---------- |
| jasontaylordev/CleanArchitecture (`v8.0.0`)   | 12.1.1                     | 12.0.1     |
| ardalis/CleanArchitecture (`v9.1.0`)          | 12.1.1/12.2                | —          |
| amantinband/clean-architecture (`main`)       | 12.2.0                     | —          |
| nadirbad/VerticalSliceArchitecture (`main`)   | 12.4.1                     | —          |
| evolutionary-architecture-by-example (`main`) | 12.5.0                     | —          |
| kgrzybek/modular-monolith-with-ddd (`master`) | 12.2.0                     | —          |
| dotnet/eShop (`main`)                         | 13.1.0 (API gelijk aan 12) | —          |
| dotnet-architecture/eShopOnWeb (`main`)       | 12.0.1                     | 12.0.1     |
| nopSolutions/nopCommerce (`release-4.70.0`)   | —                          | 13.0.1     |

Afgevallen: jbogard/ContosoUniversityDotNetCore-Pages (`main` zit op MediatR 14-beta / AutoMapper 16-beta).

## MediatR

Kolom "Patroon": welk bestaand patroon de vorm dekt. "edge" = vorm heeft een `edge-*` proof; "pending" = proof staat in `Proofs/_Pending/`.

| Vorm                                                                                         |     Treffers |                  Projecten | Patroon                                                                                    |
| -------------------------------------------------------------------------------------------- | -----------: | -------------------------: | ------------------------------------------------------------------------------------------ |
| `IRequestHandler<TReq,TRes>` direct                                                          |           38 |                          6 | `mediatr.request-handler`                                                                  |
| `IRequestHandler<TReq>` (void) direct                                                        |           17 |                          3 | `mediatr.void-request-handler`                                                             |
| `async Task(<T>) Handle`                                                                     |          273 |                          8 | request/void/notification-handler                                                          |
| `Task<T> Handle` met genest generiek resultaat (`Result<T>`, `ErrorOr<T>`)                   |         veel |                          3 | `mediatr.request-handler` (edge `edge-nested-generic`)                                     |
| handler met primary constructor                                                              |           51 |                          5 | `mediatr.request-handler` (edge `edge-primary-ctor`)                                       |
| eigen marker-interface `ICommandHandler<T> : IRequestHandler<T>` / `IQuery<T> : IRequest<T>` |       6 + 20 | 2 (kgrzybek, evolutionary) | gedekt: `edge-marker-interface` (TAAK-080)                                |
| eigen `IIntegrationEventHandler<T> : INotificationHandler<T>`                                |            4 |                          2 | gedekt: `edge-marker-interface` (TAAK-080)                                |
| abstracte/generieke handler-basisklasse                                                      |            7 |        2 (eShop, kgrzybek) | onbekend; niet gemeten                                                                     |
| `INotificationHandler<T>`                                                                    |           71 |                          7 | `mediatr.notification-handler`                                                             |
| `IPipelineBehavior<,>`                                                                       |           27 |                          6 | `mediatr.pipeline-behavior`                                                                |
| behavior `where TRequest : notnull`                                                          |            3 |                          1 | `mediatr.pipeline-behavior` (edge `edge-notnull-constraint`)                               |
| behavior `where TRequest : IRequest<TResponse>`                                              |            3 |         2 (eShop, ardalis) | gedekt, geen extra `IMessage`: `edge-request-constraint` (TAAK-081) |
| behavior met primary constructor                                                             |            3 |                          2 | `mediatr.pipeline-behavior`                                                                |
| `await next()`                                                                               |           15 |                          4 | `mediatr.pipeline-behavior`                                                                |
| `IRequestPreProcessor` / `IRequestPostProcessor`                                             |        7 / 5 |                      3 / 1 | rij 7 `mediatr.unsupported` (open)                                                         |
| `IRequestExceptionHandler` / `IRequestExceptionAction`                                       |        6 / 6 |                          2 | rij 7 `mediatr.unsupported` (open)                                                         |
| `IStreamRequest(Handler)`                                                                    |            7 |                          2 | rij 7 `mediatr.unsupported` (open)                                                         |
| `AddMediatR(cfg => ...)`                                                                     |           14 |                          7 | `mediatr.add-mediatr`                                                                      |
| `RegisterServicesFromAssemblies(...)` / `...Assembly(...)` / `...Containing(...)`            |           14 |                          7 | `mediatr.add-mediatr` (edge `edge-assemblies`)                                             |
| `cfg.AddOpenBehavior(typeof(X<,>))`                                                          |            7 |                          3 | `mediatr.add-mediatr`                                                                      |
| `cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(X<,>))`                                |            4 |                          1 | `mediatr.add-mediatr` (edge `edge-service-type-behavior`)                                  |
| `services.AddTransient(typeof(IPipelineBehavior<,>), ...)` buiten `AddMediatR`               |            5 |         2 (jason, ardalis) | niet gedekt; rij 8 in `mediatr-queue.md`                                                   |
| `cfg.Lifetime`, `NotificationPublisher`, `AddOpenRequestPre/PostProcessor`                   |            0 |                          0 | —                                                                                          |
| `IMediator` / `ISender` / `IPublisher` (injectie)                                            | 143 / 20 / 5 |                  7 / 4 / 3 | namespace-swap (TAAK-024)                                                                  |
| `await x.Send(...)`                                                                          |          145 |                          8 | niets te doen                                                                              |
| `.Send(...)` niet ge-await (mock-setup, `Task.WhenAll`, `return`)                            |            7 |     2 (eShop, amantinband) | rij 6 `mediatr.send-as-task` (open)                                                        |
| `.Publish(...)`                                                                              |           51 |                          8 | namespace-swap; niet-awaited → rij 6                                                       |
| `Send((object)...)`, `IRequest<Unit>`, `CreateStream`                                        |            0 |                          0 | —                                                                                          |

## AutoMapper

Vrijwel al het gebruik zit in nopCommerce (235 van 243 `CreateMap`); de andere twee projecten gebruiken alleen de basis.

| Vorm                                                                                                                                          | Treffers | Projecten | Opmerking                               |
| --------------------------------------------------------------------------------------------------------------------------------------------- | -------: | --------: | --------------------------------------- |
| `class X : Profile` + `CreateMap<A,B>()`                                                                                                      |  7 / 243 |         3 | kern                                    |
| `.ForMember(d => d.X, o => o.Ignore())`                                                                                                       |     1015 |         1 | nop: bijna elke `ForMember` is `Ignore` |
| `.ForMember(..., o => o.MapFrom(...))`                                                                                                        |        6 |         3 |                                         |
| `ProjectTo<T>(config)`                                                                                                                        |        3 |         1 | jason; volgens `project.md` handwerk    |
| `AddAutoMapper(...)`                                                                                                                          |        4 |         3 | DI                                      |
| `IMapper` injectie / `mapper.Map<T>(x)`                                                                                                       |   12 / 9 |       3/2 |                                         |
| `mapper.Map(src, dest)` (bestaand object)                                                                                                     |        2 |         2 |                                         |
| `new MapperConfiguration(...)` / `AssertConfigurationIsValid`                                                                                 |    3 / 2 |         2 | tests en nop-startup                    |
| `ReverseMap`, resolvers, converters, `AfterMap`, `Include(Base)`, `ConstructUsing`, `Condition`, `NullSubstitute`, `ForPath`, `ForAllMembers` |        0 |         0 | niet gezien in dit corpus               |

Kanttekening: het AutoMapper-corpus is klein en scheef (nop). Voor de geavanceerde API's is een groter corpus nodig voordat er patronen voor komen.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/automapper-queue.md`
- `docs/projectinfo/architectuur/edge-cases.md`
- `docs/projectinfo/architectuur/testen.md`
