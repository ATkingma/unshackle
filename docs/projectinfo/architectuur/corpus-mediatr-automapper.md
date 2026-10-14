# Corpus: MediatR and AutoMapper usage in open-source projects

Measured on 2026-10-11 with `grep -P` over `*.cs` (tarballs, read only; nothing copied).
Numbers are regex hits, not Roslyn analysis: indicative, not exact.

## Corpus

| Project (ref)                                 | MediatR                    | AutoMapper |
| --------------------------------------------- | -------------------------- | ---------- |
| jasontaylordev/CleanArchitecture (`v8.0.0`)   | 12.1.1                     | 12.0.1     |
| ardalis/CleanArchitecture (`v9.1.0`)          | 12.1.1/12.2                | —          |
| amantinband/clean-architecture (`main`)       | 12.2.0                     | —          |
| nadirbad/VerticalSliceArchitecture (`main`)   | 12.4.1                     | —          |
| evolutionary-architecture-by-example (`main`) | 12.5.0                     | —          |
| kgrzybek/modular-monolith-with-ddd (`master`) | 12.2.0                     | —          |
| dotnet/eShop (`main`)                         | 13.1.0 (API same as 12)    | —          |
| dotnet-architecture/eShopOnWeb (`main`)       | 12.0.1                     | 12.0.1     |
| nopSolutions/nopCommerce (`release-4.70.0`)   | —                          | 13.0.1     |

Dropped: jbogard/ContosoUniversityDotNetCore-Pages (`main` is on MediatR 14-beta / AutoMapper 16-beta).

## MediatR

Column "Pattern": which existing pattern covers the shape. "edge" = shape has an `edge-*` proof; "pending" = proof is in `Proofs/_Pending/`.

| Shape                                                                                        |         Hits |                   Projects | Pattern                                                                                    |
| -------------------------------------------------------------------------------------------- | -----------: | -------------------------: | ------------------------------------------------------------------------------------------ |
| `IRequestHandler<TReq,TRes>` direct                                                          |           38 |                          6 | `mediatr.request-handler`                                                                  |
| `IRequestHandler<TReq>` (void) direct                                                        |           17 |                          3 | `mediatr.void-request-handler`                                                             |
| `async Task(<T>) Handle`                                                                     |          273 |                          8 | request/void/notification-handler                                                          |
| `Task<T> Handle` with nested generic result (`Result<T>`, `ErrorOr<T>`)                      |         many |                          3 | `mediatr.request-handler` (edge `edge-nested-generic`)                                     |
| handler with primary constructor                                                             |           51 |                          5 | `mediatr.request-handler` (edge `edge-primary-ctor`)                                       |
| own marker interface `ICommandHandler<T> : IRequestHandler<T>` / `IQuery<T> : IRequest<T>`   |       6 + 20 | 2 (kgrzybek, evolutionary) | covered: `edge-marker-interface` (TAAK-080)                                                |
| own `IIntegrationEventHandler<T> : INotificationHandler<T>`                                  |            4 |                          2 | covered: `edge-marker-interface` (TAAK-080)                                                |
| abstract/generic handler base class                                                          |            7 |        2 (eShop, kgrzybek) | unknown; not measured                                                                      |
| `INotificationHandler<T>`                                                                    |           71 |                          7 | `mediatr.notification-handler`                                                             |
| `IPipelineBehavior<,>`                                                                       |           27 |                          6 | `mediatr.pipeline-behavior`                                                                |
| behavior `where TRequest : notnull`                                                          |            3 |                          1 | `mediatr.pipeline-behavior` (edge `edge-notnull-constraint`)                               |
| behavior `where TRequest : IRequest<TResponse>`                                              |            3 |         2 (eShop, ardalis) | covered, no extra `IMessage`: `edge-request-constraint` (TAAK-081)                         |
| behavior with primary constructor                                                            |            3 |                          2 | `mediatr.pipeline-behavior`                                                                |
| `await next()`                                                                               |           15 |                          4 | `mediatr.pipeline-behavior`                                                                |
| `IRequestPreProcessor` / `IRequestPostProcessor`                                             |        7 / 5 |                      3 / 1 | row 7 `mediatr.unsupported` (open)                                                         |
| `IRequestExceptionHandler` / `IRequestExceptionAction`                                       |        6 / 6 |                          2 | row 7 `mediatr.unsupported` (open)                                                         |
| `IStreamRequest(Handler)`                                                                    |            7 |                          2 | row 7 `mediatr.unsupported` (open)                                                         |
| `AddMediatR(cfg => ...)`                                                                     |           14 |                          7 | `mediatr.add-mediatr`                                                                      |
| `RegisterServicesFromAssemblies(...)` / `...Assembly(...)` / `...Containing(...)`            |           14 |                          7 | `mediatr.add-mediatr` (edge `edge-assemblies`)                                             |
| `cfg.AddOpenBehavior(typeof(X<,>))`                                                          |            7 |                          3 | `mediatr.add-mediatr`                                                                      |
| `cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(X<,>))`                                |            4 |                          1 | `mediatr.add-mediatr` (edge `edge-service-type-behavior`)                                  |
| `services.AddTransient(typeof(IPipelineBehavior<,>), ...)` outside `AddMediatR`              |            5 |         2 (jason, ardalis) | not covered; row 8 in `mediatr-queue.md`                                                   |
| `cfg.Lifetime`, `NotificationPublisher`, `AddOpenRequestPre/PostProcessor`                   |            0 |                          0 | —                                                                                          |
| `IMediator` / `ISender` / `IPublisher` (injection)                                           | 143 / 20 / 5 |                  7 / 4 / 3 | namespace swap (TAAK-024)                                                                  |
| `await x.Send(...)`                                                                          |          145 |                          8 | nothing to do                                                                              |
| `.Send(...)` not awaited (mock setup, `Task.WhenAll`, `return`)                              |            7 |     2 (eShop, amantinband) | row 6 `mediatr.send-as-task` (open)                                                        |
| `.Publish(...)`                                                                              |           51 |                          8 | namespace swap; not awaited → row 6                                                        |
| `Send((object)...)`, `IRequest<Unit>`, `CreateStream`                                        |            0 |                          0 | —                                                                                          |

## AutoMapper

Nearly all usage is in nopCommerce (235 of 243 `CreateMap`); the other two projects only use the basics.

| Shape                                                                                                                                         |     Hits |  Projects | Remark                                    |
| --------------------------------------------------------------------------------------------------------------------------------------------- | -------: | --------: | ----------------------------------------- |
| `class X : Profile` + `CreateMap<A,B>()`                                                                                                      |  7 / 243 |         3 | core                                      |
| `.ForMember(d => d.X, o => o.Ignore())`                                                                                                       |     1015 |         1 | nop: almost every `ForMember` is `Ignore` |
| `.ForMember(..., o => o.MapFrom(...))`                                                                                                        |        6 |         3 |                                           |
| `ProjectTo<T>(config)`                                                                                                                        |        3 |         1 | jason; manual work according to `project.md` |
| `AddAutoMapper(...)`                                                                                                                          |        4 |         3 | DI                                        |
| `IMapper` injection / `mapper.Map<T>(x)`                                                                                                      |   12 / 9 |       3/2 |                                           |
| `mapper.Map(src, dest)` (existing object)                                                                                                     |        2 |         2 |                                           |
| `new MapperConfiguration(...)` / `AssertConfigurationIsValid`                                                                                 |    3 / 2 |         2 | tests and nop startup                     |
| `ReverseMap`, resolvers, converters, `AfterMap`, `Include(Base)`, `ConstructUsing`, `Condition`, `NullSubstitute`, `ForPath`, `ForAllMembers` |        0 |         0 | not seen in this corpus                   |

Caveat: the AutoMapper corpus is small and skewed (nop). The advanced APIs need a larger corpus before patterns are made for them.

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/automapper-queue.md`
- `docs/projectinfo/architectuur/edge-cases.md`
- `docs/projectinfo/architectuur/testen.md`
