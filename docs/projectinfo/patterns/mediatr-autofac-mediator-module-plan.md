---
kingmadoc: 1
feature: "mediatr-autofac-mediator-module"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/AutofacMediatorModulePattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Core/ProjectPackages.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs
    - src/Unshackle.Tests/Proofs/MediatR/AutofacMediatorModule/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/autofac-mediator-module.before.cs
---

# Feature: mediatr.autofac-mediator-module: MediatR registered by hand in Autofac

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by KingmaDoc |

> Drawn up from TAAK-196 (kgrzybek modular-monolith, 30× `unmatched-use` in five `MediatorModule`s).

## One-sentence summary

mediatr.autofac-mediator-module: `builder.RegisterAssemblyTypes(typeof(IMediator).Assembly)` in an Autofac module becomes `AddMediator`
on its own `ServiceCollection`, with `Populate` into the `ContainerBuilder` without Mediator's handler registrations, so the handlers
stay Autofac's; `MediatR.Pipeline` types disappear from the scan lists and the processor behaviors from the registration.

## Scope (in / out)

**In scope**

- One statement per method `x.RegisterAssemblyTypes(typeof(T).Assembly | typeof(T).GetTypeInfo().Assembly).AsImplementedInterfaces()[.lifetime]()` with `T` from MediatR (Autofac method).
- In the same method: `typeof(MediatR.Pipeline.X)` as an element of an array/collection initializer; `x.RegisterGeneric(typeof(RequestPre/PostProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>))`.

**Out of scope**

- Existing pre/post processors that ran via those behaviors (Manual), exception behaviors (Manual).
- Generic decorators/handlers (TAAK-193).

## Requirements

- **REQ-1**: WHEN MediatR's assembly is scanned with Autofac THE SYSTEM SHALL replace that statement with `new ServiceCollection()`, `AddMediator(o => { o.ServiceLifetime = …; o.NotificationPublisherType = typeof(TaskWhenAllPublisher); })` and `Populate(services.Where(only Mediator/Mediator.Internals, no generic Abstractions interface))` (proof `kgrzybek-module`).
- **REQ-2**: THE SYSTEM SHALL derive the lifetime: `InstancePerLifetimeScope` → `Scoped`, none/`InstancePerDependency` → `Transient`, `SingleInstance` → `Singleton`; another call in the chain → Manual (proof `scan-lifetime`).
- **REQ-3**: THE SYSTEM SHALL remove `typeof(MediatR.Pipeline.*)` from scan lists and remove `RegisterGeneric(RequestPre/PostProcessorBehavior)` if no processor exists; if one exists → Manual (proof `with-processor`).
- **REQ-4**: WHEN the module is in another project than internal handlers THE SYSTEM SHALL (with `mediatr.internal-handler-access`) have that project give the module project `InternalsVisibleTo` (`_Combined/autofac-mediator-module`).
- **REQ-5**: THE SYSTEM SHALL add Mediator.SourceGenerator to the project with MediatR's `typeof(T).Assembly` (`ProjectPackages.TypeofAssembly`, unit test `FixAsync_AutofacScanOfMediatRAssembly_AddsSourceGenerator`).
- **REQ-6**: WHEN Autofac.Extensions.DependencyInjection or `ServiceCollection` is missing THE SYSTEM SHALL report Manual.
- **REQ-7**: WHEN a host (a project with the Mediator generator) refers to another host THE SYSTEM SHALL give the referenced host `GenerateTypesAsInternal = true` and the referring host `Assemblies = [typeof(own type), typeof(one type per referenced assembly with messages/handlers, without hosts)]`, so that the generated types of both do not clash (CS0436/CS0121) and the referring host does not name the internal handlers of the other (CS0122) (`MediatorHosts`, proof `_Combined/host-references-host`, TAAK-213).
- **REQ-8**: IF a host already gives the other host `InternalsVisibleTo` THEN THE SYSTEM SHALL report Manual: no Mediator option then prevents the clash (probe TAAK-213; `MediatorHostsTests.SettingsAsync_HostGrantsInternalsVisibleToOtherHost_IsManual`).
- **REQ-9**: WHEN a project of the solution references NetArchTest or ArchUnitNET THE SYSTEM SHALL give the Fixed rewrite a warning that archtests must exclude the namespace `Mediator` (`MediatorHosts.WithArchitectureTestWarning`, TAAK-240).

## Assumptions

- Measured (TAAK-196): MediatR 12.0–12.5 via an Autofac scan injects `TaskWhenAllPublisher`; Mediator 3.0.2 resolves handlers and behaviors per call from the container.

## Risks

- `Populate` registers `AutofacServiceProvider` as `IServiceProvider` (wins over an earlier own wrapper); both resolve from the current scope.
- A type that is also an `INotificationHandler` in the remaining Autofac loop stays one registration (Mediator's own is filtered): no duplicate.

## Models

[mediatr-autofac-mediator-module-modellen.md](mediatr-autofac-mediator-module-modellen.md).

## Open questions

- None.
