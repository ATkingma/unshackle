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

> Opgesteld uit TAAK-028.

## One-sentence summary

mediatr.add-mediatr: `services.AddMediatR(cfg => { ... })` wordt `services.AddMediator(cfg => { cfg.ServiceLifetime = ...; cfg.PipelineBehaviors = [...]; })`; wat geen directe tegenhanger heeft wordt Manual.

## Scope (in / out)

**In scope**

- `RegisterServicesFromAssembly`, `...Assemblies`, `...AssemblyContaining` vervallen: de source generator vindt handlers zelf.
- `AddOpenBehavior(typeof(X<,>))`, `AddBehavior(typeof(X))`, `AddBehavior(typeof(I), typeof(X))`, `AddBehavior<X>()`, `AddBehavior<I, X>()` → `PipelineBehaviors`.
- `cfg.Lifetime = ServiceLifetime.X` → `ServiceLifetime = ServiceLifetime.X`; zonder: `ServiceLifetime.Transient` (MediatR-standaard; Mediator's is Singleton).

**Out of scope (Manual)**

- Behaviors met lifetime-argument, `NotificationPublisher`, stream-behaviors, pre/post-processors, exception-handlers en alles wat geen `cfg.X(...)` of `cfg.Lifetime = ...` is.
- `AddMediatR` zonder lambda (bv. met een `MediatRServiceConfiguration`-variabele).

## Requirements

- **REQ-1**: WHEN `AddMediatR` een blok-lambda krijgt met registratie, lifetime en behaviors THE SYSTEM SHALL `AddMediator` met `ServiceLifetime` en `PipelineBehaviors` schrijven (proof `behaviors`).
- **REQ-2**: WHEN de lambda alleen registratie bevat (expression body) THE SYSTEM SHALL `cfg => cfg.ServiceLifetime = ServiceLifetime.Transient` schrijven (proof `expression`).
- **REQ-3**: IF een `cfg`-aanroep geen directe tegenhanger heeft (ook een behavior met lifetime) THEN THE SYSTEM SHALL Manual melden met de naam van de aanroep (proofs `unknown-call`, `behavior-lifetime`).
- **REQ-4**: IF een toewijzing anders is dan `cfg.Lifetime = ServiceLifetime.X` THEN THE SYSTEM SHALL Manual melden (proof `publisher`).
- **REQ-5**: IF `AddMediatR` geen lambda krijgt THEN THE SYSTEM SHALL Manual melden (proof `no-lambda`).
- **REQ-6**: WHEN de lambda `cfg.RegisterGenericHandlers = ...` bevat THE SYSTEM SHALL die regel laten vallen (proof `generic-handlers`; TAAK-037). Mediator registreert open generieke notification-handlers zelf en kent geen generieke request-handlers; die meldt `mediatr.unsupported`.
- **Uitbreiding TAAK-060**: WHEN `cfg.NotificationPublisher = new TaskWhenAllPublisher()` of `new ForeachAwaitPublisher()` (ook MediatR-gekwalificeerd) THE SYSTEM SHALL `cfg.NotificationPublisherType = typeof(global::Mediator.X)` schrijven (proofs `publisher`, `edge-publisher-qualified`); REQ-4 geldt nog voor een eigen publisher.
- **Uitbreiding TAAK-061/062**: WHEN `Add(Open)RequestPreProcessor`/`Add(Open)RequestPostProcessor` of `Add(Open)StreamBehavior` aangeroepen wordt THE SYSTEM SHALL ze in `PipelineBehaviors` (open pre-processor `X<>` → `X<,>`) resp. `StreamPipelineBehaviors` zetten, met gesloten exception handlers en processors uit het eigen project vooraan (proof `processors`); een leeg blok wordt `cfg => cfg.ServiceLifetime = ServiceLifetime.Transient` (proof `empty`).
- **Uitbreiding TAAK-144**: IF een weggevallen `RegisterServicesFrom...` een parameter krijgt van een buiten de assembly zichtbare methode, of een ander project scant met een niet-publieke handler, THEN THE SYSTEM SHALL Manual melden (Mediator's generator ziet de handlers van de aanroeper niet en kan internal handlers in een ander project niet aanroepen; proof `edge-assemblies-public`, `AddMediatRPatternTests`).
- **Uitbreiding TAAK-147**: IF die methode `internal` is en de assembly `InternalsVisibleTo` een andere assembly (niet `DynamicProxyGenAssembly2`), THEN THE SYSTEM SHALL ook Manual melden (proof `edge-assemblies-friend`).
- **Uitbreiding TAAK-148**: alleen Manual WHEN een ander project de methode echt aanroept (`SymbolFinder.FindReferencesAsync` via `ISolutionPattern`); de melding noemt dat project en bestand:regel. Zonder aanroep: Fixed (proof `edge-assemblies-friend`).

## Assumptions

- Mediator 3.0.2: `AddMediator(Action<MediatorOptions>)` en `MediatorOptions` (`ServiceLifetime`, `PipelineBehaviors`, `NotificationPublisherType`, ...) schrijft de source generator; hij leest de toewijzingen uit de broncode, dus die blijven letterlijke waarden.

## Risks

- `NotificationPublisher = new TaskWhenAllPublisher()` zou `NotificationPublisherType = typeof(TaskWhenAllPublisher)` kunnen worden, maar dan blijft de using `MediatR.NotificationPublishers` achter. Nu Manual.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-add-mediatr-modellen.md](mediatr-add-mediatr-modellen.md).

## Open questions

- Geen.
