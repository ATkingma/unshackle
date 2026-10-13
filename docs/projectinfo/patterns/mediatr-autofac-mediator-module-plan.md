---
kingmadoc: 1
feature: "mediatr-autofac-mediator-module"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/AutofacMediatorModulePattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Core/ProjectPackages.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs
    - src/Unshackle.Tests/Proofs/MediatR/AutofacMediatorModule/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/autofac-mediator-module.before.cs
---

# Feature: mediatr.autofac-mediator-module: MediatR met de hand in Autofac geregistreerd

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by KingmaDoc |

> Opgesteld uit TAAK-196 (kgrzybek modular-monolith, 30× `unmatched-use` in vijf `MediatorModule`s).

## One-sentence summary

mediatr.autofac-mediator-module: `builder.RegisterAssemblyTypes(typeof(IMediator).Assembly)` in een Autofac-module wordt `AddMediator`
op een eigen `ServiceCollection`, met `Populate` in de `ContainerBuilder` zonder Mediator's handler-registraties, zodat de handlers
Autofac's blijven; `MediatR.Pipeline`-typen verdwijnen uit de scanlijsten en de processor-behaviors uit de registratie.

## Scope (in / out)

**In scope**

- Eén statement per methode `x.RegisterAssemblyTypes(typeof(T).Assembly | typeof(T).GetTypeInfo().Assembly).AsImplementedInterfaces()[.lifetime]()` met `T` uit MediatR (Autofac-methode).
- In dezelfde methode: `typeof(MediatR.Pipeline.X)` als element van een array-/collectie-initializer; `x.RegisterGeneric(typeof(RequestPre/PostProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>))`.

**Out of scope**

- Bestaande pre-/postprocessors die via die behaviors draaiden (Manual), exception-behaviors (Manual).
- Generieke decorators/handlers (TAAK-193).

## Requirements

- **REQ-1**: WHEN MediatR's assembly met Autofac gescand wordt THE SYSTEM SHALL dat statement vervangen door `new ServiceCollection()`, `AddMediator(o => { o.ServiceLifetime = …; o.NotificationPublisherType = typeof(TaskWhenAllPublisher); })` en `Populate(services.Where(alleen Mediator/Mediator.Internals, geen generieke Abstractions-interface))` (proof `kgrzybek-module`).
- **REQ-2**: THE SYSTEM SHALL de lifetime afleiden: `InstancePerLifetimeScope` → `Scoped`, geen/`InstancePerDependency` → `Transient`, `SingleInstance` → `Singleton`; een andere aanroep in de keten → Manual (proof `scan-lifetime`).
- **REQ-3**: THE SYSTEM SHALL `typeof(MediatR.Pipeline.*)` uit scanlijsten halen en `RegisterGeneric(RequestPre/PostProcessorBehavior)` verwijderen als er geen processor bestaat; bestaat er een → Manual (proof `with-processor`).
- **REQ-4**: WHEN de module in een ander project staat dan internal handlers THE SYSTEM SHALL (met `mediatr.internal-handler-access`) dat project `InternalsVisibleTo` het moduleproject laten geven (`_Combined/autofac-mediator-module`).
- **REQ-5**: THE SYSTEM SHALL Mediator.SourceGenerator toevoegen aan het project met `typeof(T).Assembly` van MediatR (`ProjectPackages.TypeofAssembly`, unittest `FixAsync_AutofacScanOfMediatRAssembly_AddsSourceGenerator`).
- **REQ-6**: WHEN Autofac.Extensions.DependencyInjection of `ServiceCollection` ontbreekt THE SYSTEM SHALL Manual melden.

## Assumptions

- Gemeten (TAAK-196): MediatR 12.0–12.5 via Autofac-scan injecteert `TaskWhenAllPublisher`; Mediator 3.0.2 resolvet handlers en behaviors per aanroep uit de container.

## Risks

- `Populate` registreert `AutofacServiceProvider` als `IServiceProvider` (wint van een eerdere eigen wrapper); beide resolven uit de huidige scope.
- Een type dat in de overgebleven Autofac-lus ook `INotificationHandler` is, blijft één registratie (Mediator's eigen is gefilterd): geen dubbel.

## Modellen

[mediatr-autofac-mediator-module-modellen.md](mediatr-autofac-mediator-module-modellen.md).

## Open questions

- Geen.
