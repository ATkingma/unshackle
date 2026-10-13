# TAAK-196 — MediatR handmatig in Autofac geregistreerd (kgrzybek `MediatorModule`)

**Status**: klaar
**Aangemaakt**: 2026-10-12
**Doel**: de vijf Autofac-`MediatorModule`s van kgrzybek (30× `unmatched-use` op `MediatR.Pipeline`-typen) automatisch en gedragsgelijk naar Mediator 3.0.2.

## Context voor de volgende agent

- Patroon `mediatr.autofac-mediator-module` (`src/Unshackle.Core/Fixes/MediatR/Patterns/AutofacMediatorModulePattern.cs`), vóór `UnsupportedPattern` in `MediatRFix.Patterns`.
- Proofs: `Proofs/MediatR/AutofacMediatorModule/` (1 after, 2 manual) en `_Combined/autofac-mediator-module` (Application + Infrastructure, internal handlers).
- Probe (opgeruimd): `<probe-dir>/probe-196` met kgrzybek `91c8ef2`, Autofac 7.1.0, Autofac.Extensions.DependencyInjection 8.0.0, MediatR 12.0.1/12.2.0/12.5.0, Mediator 3.0.2.

## Analyse

kgrzybek registreert MediatR zonder `AddMediatR`: `builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly).AsImplementedInterfaces().InstancePerLifetimeScope()`
registreert MediatR's eigen klassen, een lus met `AsClosedTypesOf(...).FindConstructorsWith(new AllConstructorFinder())` de handlers
(internal, 36 met internal constructor), en `RegisterGeneric(typeof(RequestPre/PostProcessorBehavior<,>))` de processor-behaviors.
Mediator heeft geen reflectie-registratie: de dispatcher is gegenereerde code die alleen `AddMediator(IServiceCollection)` registreert.

## Metingen (probe)

| Vraag | Uitkomst |
| ----- | -------- |
| Welke `INotificationPublisher` kreeg MediatR's `Mediator` via de assembly-scan? | `TaskWhenAllPublisher` (Autofac: laatste registratie wint), in 12.0.1, 12.2.0 en 12.5.0 |
| Hoe resolvet Mediator 3.0.2 handlers? | per aanroep uit `IServiceProvider`: `GetRequiredService<IRequestHandler<,>>`, `GetServices<INotificationHandler<>>`, `GetServices<IPipelineBehavior<,>>` |
| `AddMediator` + `Populate` naast de Autofac-handlerlus | notification-handler draait 2× (dubbel geregistreerd) |
| Idem, maar `Populate` zonder Mediator's handler-descriptors | request (ook eigen `ICmdHandler<T>`, internal ctor), notification 1×, contravariante `INotificationHandler<INotification>` 1×, `IMediator` per scope gelijk, publisher `TaskWhenAllPublisher` |
| Generator in Infrastructure, handlers internal in Application | CS0122; met `InternalsVisibleTo` groen (TAAK-195's `mediatr.internal-handler-access`) |
| Generator in Application, `AddMediator` vanuit Infrastructure | runtime-fout "hasn't seen the AddMediator call": verworpen |

## Keuzes

- Scan → `new ServiceCollection()` + `AddMediator(o => { ServiceLifetime; NotificationPublisherType = TaskWhenAllPublisher })` + `builder.Populate(services.Where(alleen Mediator/Mediator.Internals, geen generieke Abstractions-interfaces))`: Autofac blijft de enige bron van handlers, met decorators, `AllConstructorFinder` en contravariantie zoals onder MediatR.
- Lifetime uit de keten: `InstancePerLifetimeScope` → `Scoped`, geen/`InstancePerDependency` → `Transient`, `SingleInstance` → `Singleton`; andere aanroepen → Manual.
- `typeof(MediatR.Pipeline.*)` uit de scanlijsten; `RegisterGeneric(RequestPre/PostProcessorBehavior)` weg als er geen processor bestaat (zonder processor no-op). Bestaat er wel een → Manual (volgorde t.o.v. andere behaviors niet exact af te leiden). Exception-behaviors geregistreerd → Manual. Exception handlers in de lijst draaiden onder MediatR nooit (geen exception-behavior) en worden na de migratie niet geregistreerd: gelijk.
- Generator: `ProjectPackages` telt `typeof(T).Assembly` van het verwijderde pakket als aanroep `typeof().Assembly` (`AddedPackage.OnlyWithCallTo`), zodat Mediator.SourceGenerator in het moduleproject komt.
- `InternalHandlerAccessPattern.HostsAsync` telt ook een project met deze module dat de assembly refereert (de generator ziet alle referenties).
- Geen Autofac.Extensions.DependencyInjection in het project → Manual (geen pakket erbij verzinnen).

## Resultaat

- kgrzybek: de 30 `unmatched-use` worden 5 Fixed-bevindingen (één per module) + `InternalsVisibleTo` in elk Application-project. Wat overblijft zijn de generieke decorators/notifications van TAAK-193.
- Bekende restverschil: `IServiceProvider` wordt `AutofacServiceProvider` (`Populate`) i.p.v. `ServiceProviderWrapper`; beide resolven uit de huidige scope.

## Zie ook

- `docs/projectinfo/patterns/mediatr-autofac-mediator-module-plan.md`
- `claude/tasks/CORPUSRUN-mediatr.md` (Herhaling na TAAK-180..184)
- `claude/tasks/mediatr-queue.md`
