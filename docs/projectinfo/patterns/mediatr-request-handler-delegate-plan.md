---
kingmadoc: 1
feature: "mediatr-request-handler-delegate"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Core/ProjectPackages.cs
    - src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.request-handler-delegate: gemockte `next` van een behavior in testcode

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

> Opgesteld uit TAAK-180 (corpusrun 2026-10-12, amantinband clean-architecture).

## One-sentence summary

Testcode die `Substitute.For<RequestHandlerDelegate<TRes>>()` configureert en aan `behavior.Handle(cmd, next, ct)` geeft, krijgt `MessageHandlerDelegate<TReq, TRes>` en `next.Invoke(Arg.Any<TReq>(), Arg.Any<CancellationToken>()).Returns(x)`; al het andere gebruik van het delegate-type is Manual met regelnummer.

## Scope (in / out)

**In scope**: een privé veld of lokale variabele van het type `RequestHandlerDelegate<T>` (of `var` met `Substitute.For`), gebruikt als `next`-argument van een `Handle` van een `IPipelineBehavior`, als `Invoke()`/`()`-ontvanger van `Returns`/`ReturnsForAnyArgs`, of in `Received()`/`DidNotReceive()`.

**Ook in scope (vangnet)**: elke MediatR-verwijzing die na alle patronen overblijft en geen finding heeft, wordt een Manual op die regel (`unmatched-use`), zodat "MediatR is still used in this project" op het projectbestand niet de enige aanwijzing is.

**Out of scope**: lambda's als `next` (`behavior.Handle(cmd, () => Task.FromResult(x), ct)`); het delegate-type van de eigen `Handle` van een behavior (dat doet `mediatr.pipeline-behavior`).

## Requirements

- **REQ-1**: WHEN een gemockt `RequestHandlerDelegate<TRes>` aan een behavior-`Handle` wordt gegeven THE SYSTEM SHALL het type `MessageHandlerDelegate<TReq, TRes>` maken (TReq uit de `Handle`-parameter), `Invoke()` van `Arg.Any` voorzien en `Task.FromResult(x)` in `Returns` naar `ValueTask.FromResult(x)` zetten (proof `_Combined/request-handler-delegate`).
- **REQ-2**: IF het delegate een echt delegate is (lambda), een parameter, direct wordt aangeroepen, of `Returns` een Task/callback krijgt THEN THE SYSTEM SHALL Manual melden met de regel (proofs `lambda`, `parameter`, `invoke-call`, `callback-returns`).
- **REQ-3**: IF het request-type niet uit precies één `Handle`-aanroep volgt THEN THE SYSTEM SHALL Manual melden (proof `no-handle-call`).
- **REQ-4**: WHEN na de patronen nog een MediatR-symbool zonder finding in een project staat dat het pakket houdt THE SYSTEM SHALL een Manual op die regel melden (unittest `FixerProjectFileTests`).

## Assumptions

- Mediator 3.0.2: `delegate ValueTask<TResponse> MessageHandlerDelegate<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken)`; NSubstitute 6 configureert `ValueTask<T>` met `Returns(T)`.
- Het patroon staat vóór `mediatr.pipeline-behavior` in `MediatRFix.Patterns`: staan behavior en test in één bestand, dan bindt de `Handle`-aanroep nog aan de MediatR-versie.
- Per bestand wordt tegen de originele solution gefixt; de behavior in een ander bestand is dus nog de MediatR-versie.

## Modellen

Beslisboom en stappen: [mediatr-request-handler-delegate-modellen.md](mediatr-request-handler-delegate-modellen.md).

## Zie ook

- [mediatr-request-handler-delegate-verify.md](mediatr-request-handler-delegate-verify.md)
- `claude/tasks/archief/TAAK-180-mediatr-test-request-handler-delegate.md`
- [mediatr-pipeline-behavior-plan.md](mediatr-pipeline-behavior-plan.md)
