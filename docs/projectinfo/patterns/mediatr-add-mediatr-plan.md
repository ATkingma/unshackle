---
kingmadoc: 1
feature: "mediatr-add-mediatr"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/AddMediatR/
---

# Feature: mediatr.add-mediatr: AddMediatR → AddMediator

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-028.

## One-sentence summary

mediatr.add-mediatr: `services.AddMediatR(cfg => { ... })` becomes `services.AddMediator(cfg => { cfg.ServiceLifetime = ...; cfg.PipelineBehaviors = [...]; })`; anything without a direct counterpart becomes Manual.

## Scope (in / out)

**In scope**

- `RegisterServicesFromAssembly`, `...Assemblies`, `...AssemblyContaining` are dropped: the source generator finds handlers itself.
- `AddOpenBehavior(typeof(X<,>))`, `AddBehavior(typeof(X))`, `AddBehavior(typeof(I), typeof(X))`, `AddBehavior<X>()`, `AddBehavior<I, X>()` → `PipelineBehaviors`.
- `cfg.Lifetime = ServiceLifetime.X` → `ServiceLifetime = ServiceLifetime.X`; without it: `ServiceLifetime.Transient` (MediatR default; Mediator's is Singleton).

**Out of scope (Manual)**

- Behaviors with a lifetime argument, `NotificationPublisher`, stream behaviors, pre/post-processors, exception handlers and anything that is not `cfg.X(...)` or `cfg.Lifetime = ...`.
- `AddMediatR` without a lambda (e.g. with a `MediatRServiceConfiguration` variable).

## Requirements

- **REQ-1**: WHEN `AddMediatR` gets a block lambda with registration, lifetime and behaviors THE SYSTEM SHALL write `AddMediator` with `ServiceLifetime` and `PipelineBehaviors` (proof `behaviors`).
- **REQ-2**: WHEN the lambda contains only registration (expression body) THE SYSTEM SHALL write `cfg => cfg.ServiceLifetime = ServiceLifetime.Transient` (proof `expression`). **REQ-2b**: WHEN an expression-bodied lambda needs several assignments THE SYSTEM SHALL turn it into a block body (proof `expression-several`, TAAK-270).
- **REQ-3**: IF a `cfg` call has no direct counterpart (including a behavior with a lifetime) THEN THE SYSTEM SHALL report Manual with the name of the call (proofs `unknown-call`, `behavior-lifetime`).
- **REQ-4**: IF an assignment is anything other than `cfg.Lifetime = ServiceLifetime.X` THEN THE SYSTEM SHALL report Manual (proof `publisher`).
- **REQ-5**: IF `AddMediatR` gets no lambda THEN THE SYSTEM SHALL report Manual (proof `no-lambda`).
- **REQ-6**: WHEN the lambda contains `cfg.RegisterGenericHandlers = ...` THE SYSTEM SHALL drop that line (proof `generic-handlers`; TAAK-037). Mediator registers open generic notification handlers itself and has no generic request handlers; those are reported by `mediatr.unsupported`.
- **REQ-7**: WHEN the host references another host, or another host references it THE SYSTEM SHALL add `cfg.Assemblies = [...]` or `cfg.GenerateTypesAsInternal = true` respectively (`MediatorHosts`); IF `AddMediatR` scans another host THEN Manual (`MediatorHostsTests.FixAsync_AddMediatRHostReferencesHost_InternalTypesAndAssemblies`, `..._AddMediatRScansOtherHost_IsManual`; TAAK-213).
- **Extension TAAK-060**: WHEN `cfg.NotificationPublisher = new TaskWhenAllPublisher()` or `new ForeachAwaitPublisher()` (also MediatR-qualified) THE SYSTEM SHALL write `cfg.NotificationPublisherType = typeof(global::Mediator.X)` (proofs `publisher`, `edge-publisher-qualified`); REQ-4 still applies to an own publisher.
- **REQ-8 (TAAK-290)**: IF a non-public method scans an assembly parameter that, through calls in the solution, comes from a parameter of a public method THEN THE SYSTEM SHALL report Manual naming that public method (the assembly is only known at run time); this run-time scan reason SHALL come before a host-clash reason from `MediatorHosts` (`AddMediatRPatternTests.RewriteAsync_InternalScanFedByPublicParameter_ManualNamingThePublicMethod`; corpus starter).
- **Extension TAAK-061/062**: WHEN `Add(Open)RequestPreProcessor`/`Add(Open)RequestPostProcessor` or `Add(Open)StreamBehavior` is called THE SYSTEM SHALL put them in `PipelineBehaviors` (open pre-processor `X<>` → `X<,>`) or `StreamPipelineBehaviors` respectively, with closed exception handlers and processors from the own project first (proof `processors`); an empty block becomes `cfg => cfg.ServiceLifetime = ServiceLifetime.Transient` (proof `empty`).
- **Extension TAAK-144**: IF a dropped `RegisterServicesFrom...` gets a parameter of a method visible outside the assembly, or scans another project with a non-public handler, THEN THE SYSTEM SHALL report Manual (Mediator's generator does not see the caller's handlers and cannot call internal handlers in another project; proof `edge-assemblies-public`, `AddMediatRPatternTests`).
- **Extension TAAK-183**: IF the scanned assembly with a non-public handler grants this project `InternalsVisibleTo`, THEN THE SYSTEM SHALL simply rewrite the call (Mediator 3.0.2 then reaches the internal handlers; measured with a probe). A public method with an `Assembly` parameter (evo2) and an internal method that a friend project actually calls (evo1) stay Manual: `MediatorOptions.Assemblies` only limits which referenced assemblies the generator scans, and multiple `AddMediator` calls give one `IMediator` (TAAK-144). `AddMediatRPatternTests`.
- **Extension TAAK-195**: WHEN the scanned assembly with a non-public handler is a C# project of the solution without a strong name THE SYSTEM SHALL rewrite the call; `mediatr.internal-handler-access` gives that project `InternalsVisibleTo` for this project (proof `_Combined/add-mediatr-internal-handlers-other-project`, ardalis). Without a solution (`Rewrite`) it stays Manual. The public method with an `Assembly` parameter (evo2) and the internal method called by a test project (evo1) stay Manual: no exact translation (see TAAK-195).
- **Extension TAAK-147**: IF that method is `internal` and the assembly has `InternalsVisibleTo` for another assembly (not `DynamicProxyGenAssembly2`), THEN THE SYSTEM SHALL also report Manual (proof `edge-assemblies-friend`).
- **Extension TAAK-240**: WHEN a project of the solution references NetArchTest or ArchUnitNET THE SYSTEM SHALL give the Fixed rewrite a warning: arch tests must exclude namespace `Mediator` (`.And().DoNotResideInNamespace("Mediator")`), because the generated `Mediator.Mediator`/`Mediator.Internals.ContainerMetadata` mention messages of other modules (TAAK-214; `MediatorHosts.WithArchitectureTestWarning`).
- **Extension TAAK-148**: only Manual WHEN another project actually calls the method (`SymbolFinder.FindReferencesAsync` via `ISolutionPattern`); the message names that project and file:line. Without a call: Fixed (proof `edge-assemblies-friend`).

## Assumptions

- Mediator 3.0.2: `AddMediator(Action<MediatorOptions>)` and `MediatorOptions` (`ServiceLifetime`, `PipelineBehaviors`, `NotificationPublisherType`, ...) are written by the source generator; it reads the assignments from the source code, so they stay literal values.

## Risks

- `NotificationPublisher = new TaskWhenAllPublisher()` could become `NotificationPublisherType = typeof(TaskWhenAllPublisher)`, but then the using `MediatR.NotificationPublishers` is left behind. Manual for now.

## Models

Class diagram, sequence and decision tree: [mediatr-add-mediatr-modellen.md](mediatr-add-mediatr-modellen.md).

## Open questions

- None.
