---
kingmadoc: 1
feature: "mediatr-generic-decorator"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/GenericDecorator/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/generic-decorators-kgrzybek.before.cs
---

# Feature: mediatr.generic-decorator: open generieke handler-decorator blijft container-decorator

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand) |

> Opgesteld uit TAAK-193 (kgrzybek modular-monolith: zes decorators per module, Autofac `RegisterGenericDecorator`).

## One-sentence summary

Een open generieke decorator `D<T> : ICommandHandler<T>` met een constructorparameter `ICommandHandler<T> decorated` blijft een Autofac-decorator;
bij een void-handler wordt de gedecoreerde parameter `IRequestHandler<T, Unit>` en het servicetype van `RegisterGenericDecorator` `typeof(IRequestHandler<,>)`,
omdat Mediator een void-handler als `IRequestHandler<T, Unit>` uit DI haalt.

## Scope (in / out)

**In**: niet-abstracte open generieke klasse in de bron die MediatR `IRequestHandler<T>`, `IRequestHandler<T, R>` of `INotificationHandler<T>` over haar eigen typeparameter
implementeert én een constructorparameter van die handler (of een afgeleide interface) heeft; `typeof(S<>)` als tweede argument van Autofac's `RegisterGenericDecorator`.

**Out (Manual)**: decorator die ergens anders gebruikt wordt dan als `typeof(D<>)` in `RegisterGenericDecorator`; eigen handler-interface met eigen members;
gedecoreerde handler die anders gebruikt wordt dan opslaan, toewijzen en `Handle` aanroepen; notification-decorator zonder handmatige MediatR-registratie in Autofac.
Een open generieke handler zónder gedecoreerde parameter blijft bij `mediatr.unsupported`.

## Requirements

- **REQ-1**: WHEN een void-decorator alleen via `RegisterGenericDecorator(typeof(D<>), typeof(S<>))` geregistreerd is THE SYSTEM SHALL velden, parameters en locals van het gedecoreerde type `IRequestHandler<T, Unit>` maken en `typeof(S<>)` → `typeof(IRequestHandler<,>)` (proof `_Combined/generic-decorators-kgrzybek`).
- **REQ-2**: WHEN de decorator een handler met response decoreert THE SYSTEM SHALL hem niet vinden; de handler-patronen doen `Task` → `ValueTask` (unittest `Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns`).
- **REQ-3**: THE SYSTEM SHALL MSG0003 in `WarningsNotAsErrors` zetten: de generator slaat de decorator alleen over (`MediatRFix`).
- **REQ-4**: IF de decorator anders gebruikt wordt dan in `RegisterGenericDecorator` THEN Manual (proof `used-elsewhere`).
- **REQ-5**: IF de eigen handler-interface members heeft, of de gedecoreerde handler anders gebruikt wordt dan `Handle` aanroepen THEN Manual (proofs `decorated-members`, `decorated-other-use`).
- **REQ-6**: WHEN een notification-decorator gevonden wordt THE SYSTEM SHALL alleen Fixed geven als MediatR met de hand in Autofac geregistreerd is (`mediatr.autofac-mediator-module`), anders Manual (proof `notification-without-autofac`).

## Assumptions

- Gemeten (probes p1–p3, TAAK-193): Mediator 3.0.2 resolvet handlers per aanroep uit DI, dus Autofac's decorators wikkelen ze zoals onder MediatR (laatst geregistreerd = buitenste).
- Met `AddMediator` registreert de generator een open generieke notification-handler voor élke notification: een decorator zou zichzelf decoreren (probe bleef hangen).

## Modellen

[mediatr-generic-decorator-modellen.md](mediatr-generic-decorator-modellen.md).

## Zie ook

- [mediatr-generic-decorator-verify.md](mediatr-generic-decorator-verify.md)
- `claude/tasks/archief/TAAK-193-mediatr-generieke-decorators-messages.md`
