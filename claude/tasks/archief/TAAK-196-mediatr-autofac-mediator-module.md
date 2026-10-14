# TAAK-196 — MediatR registered by hand in Autofac (kgrzybek `MediatorModule`)

**Status**: done
**Created**: 2026-10-12
**Goal**: migrate kgrzybek's five Autofac `MediatorModule`s (30× `unmatched-use` on `MediatR.Pipeline` types) automatically and with equal behaviour to Mediator 3.0.2.

## Context for the next agent

- Pattern `mediatr.autofac-mediator-module` (`src/Unshackle.Core/Fixes/MediatR/Patterns/AutofacMediatorModulePattern.cs`), before `UnsupportedPattern` in `MediatRFix.Patterns`.
- Proofs: `Proofs/MediatR/AutofacMediatorModule/` (1 after, 2 manual) and `_Combined/autofac-mediator-module` (Application + Infrastructure, internal handlers).
- Probe (cleaned up): `<probe-dir>/probe-196` with kgrzybek `91c8ef2`, Autofac 7.1.0, Autofac.Extensions.DependencyInjection 8.0.0, MediatR 12.0.1/12.2.0/12.5.0, Mediator 3.0.2.

## Analysis

kgrzybek registers MediatR without `AddMediatR`: `builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly).AsImplementedInterfaces().InstancePerLifetimeScope()`
registers MediatR's own classes, a loop with `AsClosedTypesOf(...).FindConstructorsWith(new AllConstructorFinder())` the handlers
(internal, 36 with an internal constructor), and `RegisterGeneric(typeof(RequestPre/PostProcessorBehavior<,>))` the processor behaviors.
Mediator has no reflection registration: the dispatcher is generated code that only registers `AddMediator(IServiceCollection)`.

## Measurements (probe)

| Question | Outcome |
| -------- | ------- |
| Which `INotificationPublisher` did MediatR's `Mediator` get via the assembly scan? | `TaskWhenAllPublisher` (Autofac: the last registration wins), in 12.0.1, 12.2.0 and 12.5.0 |
| How does Mediator 3.0.2 resolve handlers? | per call from `IServiceProvider`: `GetRequiredService<IRequestHandler<,>>`, `GetServices<INotificationHandler<>>`, `GetServices<IPipelineBehavior<,>>` |
| `AddMediator` + `Populate` next to the Autofac handler loop | the notification handler runs 2× (registered twice) |
| Same, but `Populate` without Mediator's handler descriptors | request (also own `ICmdHandler<T>`, internal ctor), notification 1×, contravariant `INotificationHandler<INotification>` 1×, `IMediator` equal per scope, publisher `TaskWhenAllPublisher` |
| Generator in Infrastructure, handlers internal in Application | CS0122; with `InternalsVisibleTo` green (TAAK-195's `mediatr.internal-handler-access`) |
| Generator in Application, `AddMediator` from Infrastructure | runtime error "hasn't seen the AddMediator call": rejected |

## Choices

- Scan → `new ServiceCollection()` + `AddMediator(o => { ServiceLifetime; NotificationPublisherType = TaskWhenAllPublisher })` + `builder.Populate(services.Where(only Mediator/Mediator.Internals, no generic Abstractions interfaces))`: Autofac stays the only source of handlers, with decorators, `AllConstructorFinder` and contravariance as under MediatR.
- Lifetime from the chain: `InstancePerLifetimeScope` → `Scoped`, none/`InstancePerDependency` → `Transient`, `SingleInstance` → `Singleton`; other calls → Manual.
- `typeof(MediatR.Pipeline.*)` out of the scan lists; `RegisterGeneric(RequestPre/PostProcessorBehavior)` removed if no processor exists (without a processor a no-op). If one exists → Manual (the order relative to other behaviors cannot be derived exactly). Exception behaviors registered → Manual. Exception handlers in the list never ran under MediatR (no exception behavior) and are not registered after the migration: equal.
- Generator: `ProjectPackages` counts `typeof(T).Assembly` of the removed package as a `typeof().Assembly` call (`AddedPackage.OnlyWithCallTo`), so Mediator.SourceGenerator goes into the module project.
- `InternalHandlerAccessPattern.HostsAsync` also counts a project with this module that references the assembly (the generator sees all references).
- No Autofac.Extensions.DependencyInjection in the project → Manual (do not invent a package).

## Result

- kgrzybek: the 30 `unmatched-use` become 5 Fixed findings (one per module) + `InternalsVisibleTo` in every Application project. What remains are the generic decorators/notifications of TAAK-193.
- Known remaining difference: `IServiceProvider` becomes `AutofacServiceProvider` (`Populate`) instead of `ServiceProviderWrapper`; both resolve from the current scope.

## See also

- `docs/projectinfo/patterns/mediatr-autofac-mediator-module-plan.md`
- `claude/tasks/CORPUSRUN-mediatr.md` (Repeat after TAAK-180..184)
- `claude/tasks/mediatr-queue.md`
