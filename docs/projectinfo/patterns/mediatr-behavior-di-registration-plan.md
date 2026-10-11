---
kingmadoc: 1
feature: "mediatr-behavior-di-registration"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/BehaviorDiRegistrationPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/BehaviorDiRegistration/
---

# Feature: mediatr.behavior-di-registration: behavior-registratie in DI blijft staan

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-145.

## One-sentence summary

mediatr.behavior-di-registration: `services.AddTransient/AddScoped/AddSingleton(typeof(IPipelineBehavior<,>), typeof(X<,>))` blijft ongewijzigd, omdat Mediator 3 behaviors uit de container leest; de using-swap maakt er `Mediator.IPipelineBehavior` van.

## Scope (in / out)

**In scope**

- `AddTransient`, `AddScoped`, `AddSingleton` uit `Microsoft.Extensions.DependencyInjection` met twee `typeof`-argumenten,
  waarvan de eerste `MediatR.IPipelineBehavior<,>` is (open of gesloten generiek).

**Out of scope**

- `cfg.AddBehavior(...)`/`AddOpenBehavior(...)` binnen `AddMediatR`: dat doet `mediatr.add-mediatr`.
- De generieke vorm `AddTransient<IPipelineBehavior<A,B>, X>()` en factory-overloads: werken via dezelfde swap, geen finding.

## Requirements

- **REQ-1**: WHEN een behavior met `AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` geregistreerd wordt THE SYSTEM SHALL de aanroep laten staan en alleen `using MediatR` → `using Mediator` swappen (proof `transient`).
- **REQ-2**: WHEN dat met `AddScoped`, `AddSingleton` of een gesloten `IPipelineBehavior<A,B>` gebeurt THE SYSTEM SHALL hetzelfde doen (proof `lifetimes`).
- **REQ-3**: THE SYSTEM SHALL nooit Manual melden: gemeten in TAAK-145 draait de gegenereerde Mediator 3.0.2 elke in DI geregistreerde behavior.

## Assumptions

- Mediator 3.0.2 resolvet `IPipelineBehavior<TMessage,TResponse>` uit de `IServiceProvider` (gemeten met een echte test in `/mnt/dikkeschijf/unshackle-corpus/t145/`).

## Risks

- Zette de gebruiker in `AddMediatR` `Lifetime = ServiceLifetime.Singleton`, dan cachet Mediator de behavior-instanties:
  een transient behavior wordt dan per handler één instantie (gemeten). MediatR maakte er per `Send` een nieuwe. Geen
  waarschuwing: dat vraagt analyse over bestanden heen; de standaard (`Transient`) heeft het verschil niet.

## Modellen

Klassendiagram, sequence en beslisboom: [mediatr-behavior-di-registration-modellen.md](mediatr-behavior-di-registration-modellen.md).

## Open questions

- Geen.
