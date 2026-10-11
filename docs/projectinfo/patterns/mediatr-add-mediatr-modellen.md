# Modellen: mediatr.add-mediatr

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`.

## Figuur 1: klassen

**Figure 1.** `AddMediatRPattern` en de MediatR- en Mediator-registratie.

<!-- kingmadoc:diagram img/mediatr-add-mediatr-modellen-1.d2 -->
![Figuur 1: klassen](img/mediatr-add-mediatr-modellen-1.png)

| Element             | Rol                                                                           |
| ------------------- | ----------------------------------------------------------------------------- |
| `AddMediatRPattern` | Vindt `AddMediatR` (symbool uit assembly `MediatR`) en schrijft `AddMediator` |
| `s_dropped`         | `RegisterServicesFrom*`: vervallen, de source generator vindt handlers        |
| `Behavior`          | `AddOpenBehavior`/`AddBehavior` → `typeof(...)` van de implementatie          |
| `MediatorOptions`   | Door de generator geschreven; `ServiceLifetime`, `PipelineBehaviors`          |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Fixed` of `Manual`.

<!-- kingmadoc:diagram img/mediatr-add-mediatr-modellen-2.d2 -->
![Figuur 2: sequence](img/mediatr-add-mediatr-modellen-2.png)

| Stap         | Methode in de code                       | Wat gebeurt er                                          |
| ------------ | ---------------------------------------- | ------------------------------------------------------- |
| Vinden       | `Find` met `GetSymbolInfo`               | Alleen de echte `AddMediatR` van MediatR                |
| Instellingen | `ParameterName`, `Behavior`, `s_dropped` | Elke instelling: vervalt, behavior, lifetime of Manual  |
| Schrijven    | `SyntaxFactory.ParseStatement`           | Letterlijke toewijzingen, inspringing van het origineel |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Rewrite`.

<!-- kingmadoc:diagram img/mediatr-add-mediatr-modellen-3.d2 -->
![Figuur 3: beslisboom](img/mediatr-add-mediatr-modellen-3.png)

| Blad                                          | Voorwaarde                             | Bewijs                              |
| --------------------------------------------- | -------------------------------------- | ----------------------------------- |
| Manual "not called as AddMediatR(cfg => ...)" | geen lambda                            | `AddMediatR/no-lambda`              |
| Manual "not a list of cfg calls"              | blok met andere statements             | geen proof                          |
| Manual "cfg.X(...) has no direct equivalent"  | onbekende aanroep of lifetime-argument | `unknown-call`, `behavior-lifetime` |
| Manual "'...' has no direct equivalent"       | andere toewijzing/expressie            | `publisher`                         |
| Manual "behaviors in expression lambda"       | expression body met behavior           | geen proof                          |
| `AddMediator(cfg => ...)`                     | alles bekend                           | `behaviors`, `expression`           |

## Zie ook

- [mediatr-add-mediatr-plan.md](mediatr-add-mediatr-plan.md)
- [mediatr-add-mediatr-verify.md](mediatr-add-mediatr-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/AddMediatR/`
