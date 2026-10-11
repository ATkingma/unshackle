# Modellen: mediatr.notification-handler

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/NotificationHandlerPattern.cs` en `MediatRSyntax.cs`.

## Figuur 1: klassen

**Figure 1.** `NotificationHandlerPattern`, zijn helper en de MediatR- en Mediator-typen.

<!-- kingmadoc:diagram img/mediatr-notification-handler-modellen-1.d2 -->
![Figuur 1: klassen](img/mediatr-notification-handler-modellen-1.png)

| Element                      | Rol                                                                                      |
| ---------------------------- | ---------------------------------------------------------------------------------------- |
| `NotificationHandlerPattern` | Vindt `Handle` van `MediatR.INotificationHandler<T>` en maakt er `ValueTask Handle` van   |
| `MediatRSyntax`              | Gedeelde helper: vinden, `Task`→`ValueTask`, returns, `Task.X` → `ValueTask.X`            |
| `Mediator.INotificationHandler<T>` | Zelfde naam (in `SwappedTypes`), `Handle` geeft `ValueTask`                         |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/mediatr-notification-handler-modellen-2.d2 -->
![Figuur 2: sequence](img/mediatr-notification-handler-modellen-2.png)

| Stap       | Methode in de code                                         | Wat gebeurt er                                             |
| ---------- | ---------------------------------------------------------- | ---------------------------------------------------------- |
| Vinden     | `MediatRSyntax.FindHandleMethods` (`INotificationHandler<>`) | Semantisch de `Handle` die het interface-lid implementeert |
| Returntype | `MediatRSyntax.ToValueTask`                                | `Task` → `ValueTask`                                       |
| niet-async | `ReturnedExpressions`, `IsTaskMember`, `OnValueTask`       | `Task.CompletedTask` → `ValueTask.CompletedTask`           |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-notification-handler-modellen-3.d2 -->
![Figuur 3: beslisboom](img/mediatr-notification-handler-modellen-3.png)

| Blad                                | Voorwaarde                                    | Bewijs                                                                  |
| ----------------------------------- | --------------------------------------------- | ----------------------------------------------------------------------- |
| Manual "explicit interface"         | `Task INotificationHandler<T>.Handle`         | `NotificationHandler/explicit`                                          |
| Manual "does not return Task"       | returntype niet in de vorm `Task` (bv. alias) | geen proof                                                              |
| `ValueTask`, body blijft            | `async`                                       | `NotificationHandler/async`, `two-handlers`                             |
| `ValueTask` + `ValueTask.CompletedTask` | niet-async, alle returns `Task.CompletedTask` | `NotificationHandler/completed-task`, `completed-task-expression`, `two-handlers` |
| Manual "not Task.CompletedTask"     | andere return, bv. `Task.Delay`               | `NotificationHandler/other-return`                                      |

## Zie ook

- [mediatr-notification-handler-plan.md](mediatr-notification-handler-plan.md)
- [mediatr-notification-handler-verify.md](mediatr-notification-handler-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/NotificationHandlerPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/NotificationHandler/`
