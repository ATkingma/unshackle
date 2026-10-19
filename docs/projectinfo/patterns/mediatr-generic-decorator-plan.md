---
kingmadoc: 1
feature: "mediatr-generic-decorator"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/GenericDecorator/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/generic-decorators-kgrzybek.before.cs
---

# Feature: mediatr.generic-decorator: an open generic handler decorator stays a container decorator

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand)    |

> Drawn up from TAAK-193 (kgrzybek modular-monolith: six decorators per module, Autofac `RegisterGenericDecorator`).

## One-sentence summary

An open generic decorator `D<T> : ICommandHandler<T>` with a constructor parameter `ICommandHandler<T> decorated` stays an Autofac decorator;
for a void handler the decorated parameter becomes `IRequestHandler<T, Unit>` and the service type of `RegisterGenericDecorator` `typeof(IRequestHandler<,>)`,
because Mediator gets a void handler from DI as `IRequestHandler<T, Unit>`.

## Scope (in / out)

**In**: a non-abstract open generic class in the source that implements MediatR `IRequestHandler<T>`, `IRequestHandler<T, R>` or `INotificationHandler<T>` over its own type parameter
and has a constructor parameter of that handler (or a derived interface); `typeof(S<>)` as the second argument of Autofac's `RegisterGenericDecorator`.

**Out (Manual)**: a decorator that is used anywhere other than as `typeof(D<>)` in `RegisterGenericDecorator`; an own handler interface with own members;
a decorated handler that is used other than stored, assigned and `Handle` called; a notification decorator without manual MediatR registration in Autofac.
An open generic handler **without** a decorated parameter stays with `mediatr.unsupported`.

## Requirements

- **REQ-1**: WHEN a void decorator is only registered via `RegisterGenericDecorator(typeof(D<>), typeof(S<>))` THE SYSTEM SHALL make fields, parameters and locals of the decorated type `IRequestHandler<T, Unit>` and `typeof(S<>)` → `typeof(IRequestHandler<,>)` (proof `_Combined/generic-decorators-kgrzybek`).
- **REQ-2**: WHEN the decorator decorates a handler with a response THE SYSTEM SHALL not find it; the handler patterns do `Task` → `ValueTask` (unit test `Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns`).
- **REQ-3**: THE SYSTEM SHALL put MSG0003 in `WarningsNotAsErrors`: the generator only skips the decorator (`MediatRFix`).
- **REQ-4**: IF the decorator is used other than in `RegisterGenericDecorator` THEN Manual (proof `used-elsewhere`).
- **REQ-5**: IF the own handler interface has members, or the decorated handler is used other than calling `Handle` THEN Manual (proofs `decorated-members`, `decorated-other-use`).
- **REQ-6**: WHEN a notification decorator is found THE SYSTEM SHALL only give Fixed if MediatR is registered by hand in Autofac (`mediatr.autofac-mediator-module`), otherwise Manual (proof `notification-without-autofac`).
- **REQ-7** (TAAK-210): WHEN the decorator only implements an own marker (`ICommandHandler<T> : IRequestHandler<T>`) and its file has no `using MediatR` THE SYSTEM SHALL still recognise it as a decorator after its own rewrite (`IRequestHandler<T, Unit>`, only binds after `using Mediator`), so that `mediatr.unsupported` does not report it as a generic handler (proof `_Combined/generic-decorator-marker-without-using`, unit tests `IsDecorator_RewrittenVoidDecoratorInFileWithoutUsingMediatR_True`, `IsDecorator_UnboundHandlerWithOtherResponse_False`).
- **REQ-8** (council round 3, fullcart): IF the decorator is registered with Scrutor's `Decorate(typeof(S<>), typeof(D<>))` THEN Manual, and the reason SHALL name that Scrutor call with file:line instead of Autofac's `RegisterGenericDecorator` (proof `scrutor-decorate`).
- **REQ-9** (council round 4, Q4): THE SYSTEM SHALL name a `Decorate` call as Scrutor's only when it resolves to a method of Scrutor (the `Scrutor`
  namespace, or the `Scrutor` assembly, which declares `Decorate` in `Microsoft.Extensions.DependencyInjection`); the syntax match SHALL be used only
  when the symbol does not resolve. A user's own `Decorate` SHALL get the plain "used other than in Autofac's RegisterGenericDecorator" reason
  (unit test `RewriteAsync_DecorateCall_NamesScrutorOnlyWhenItResolvesThereOrNotAtAll`).

## Assumptions

- Measured (probes p1–p3, TAAK-193): Mediator 3.0.2 resolves handlers per call from DI, so Autofac's decorators wrap them as under MediatR (last registered = outermost).
- With `AddMediator` the generator registers an open generic notification handler for **every** notification: a decorator would decorate itself (the probe hung).

## Models

[mediatr-generic-decorator-modellen.md](mediatr-generic-decorator-modellen.md).

## See also

- [mediatr-generic-decorator-verify.md](mediatr-generic-decorator-verify.md)
- `claude/tasks/archief/TAAK-193-mediatr-generieke-decorators-messages.md`
