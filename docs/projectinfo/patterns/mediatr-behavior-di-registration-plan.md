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

# Feature: mediatr.behavior-di-registration: behavior registration in DI stays as it is

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-145.

## One-sentence summary

mediatr.behavior-di-registration: `services.AddTransient/AddScoped/AddSingleton(typeof(IPipelineBehavior<,>), typeof(X<,>))` stays unchanged, because Mediator 3 reads behaviors from the container; the using swap turns it into `Mediator.IPipelineBehavior`.

## Scope (in / out)

**In scope**

- `AddTransient`, `AddScoped`, `AddSingleton` from `Microsoft.Extensions.DependencyInjection` with two `typeof` arguments,
  of which the first is `MediatR.IPipelineBehavior<,>` (open or closed generic).

**Out of scope**

- `cfg.AddBehavior(...)`/`AddOpenBehavior(...)` inside `AddMediatR`: that is done by `mediatr.add-mediatr`.
- The generic form `AddTransient<IPipelineBehavior<A,B>, X>()` and factory overloads: work via the same swap, no finding.

## Requirements

- **REQ-1**: WHEN a behavior is registered with `AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` THE SYSTEM SHALL leave the call and only swap `using MediatR` → `using Mediator` (proof `transient`).
- **REQ-2**: WHEN that happens with `AddScoped`, `AddSingleton` or a closed `IPipelineBehavior<A,B>` THE SYSTEM SHALL do the same (proof `lifetimes`).
- **REQ-3**: THE SYSTEM SHALL never report Manual: measured in TAAK-145, the generated Mediator 3.0.2 runs every behavior registered in DI.

## Assumptions

- Mediator 3.0.2 resolves `IPipelineBehavior<TMessage,TResponse>` from the `IServiceProvider` (measured with a real test in `<corpus>/t145/`).

## Risks

- If the user set `Lifetime = ServiceLifetime.Singleton` in `AddMediatR`, Mediator caches the behavior instances:
  a transient behavior then becomes one instance per handler (measured). MediatR created a new one per `Send`. No
  warning: that requires analysis across files; the default (`Transient`) does not have the difference.

## Models

Class diagram, sequence and decision tree: [mediatr-behavior-di-registration-modellen.md](mediatr-behavior-di-registration-modellen.md).

## Open questions

- None.
