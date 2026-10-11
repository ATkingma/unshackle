# Modellen: mediatr.unsupported

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`.

## Figuur 1: klassen

**Figure 1.** `UnsupportedPattern`, de herkende MediatR-interfaces en de Mediator-tegenhangers uit het advies.

<!-- kingmadoc:diagram img/mediatr-unsupported-modellen-1.d2 -->
![Figuur 1: klassen](img/mediatr-unsupported-modellen-1.png)

| Element              | Rol                                                                  |
| -------------------- | -------------------------------------------------------------------- |
| `UnsupportedPattern` | Vindt typedeclaraties met een MediatR-interface zonder tweeling      |
| `s_advice`           | Metadata-naam → adviestekst (pre/post, exception, publisher, stream) |
| `s_noGeneric`        | Berichten en request-handlers die niet generiek mogen zijn           |
| `PatternResult`      | Altijd `Manual(advies)`                                              |

## Figuur 2: sequence

**Figure 2.** Van `Fixer` via `Find` en `Rewrite` naar `Manual`.

<!-- kingmadoc:diagram img/mediatr-unsupported-modellen-2.d2 -->
![Figuur 2: sequence](img/mediatr-unsupported-modellen-2.png)

| Stap   | Methode in de code                        | Wat gebeurt er                               |
| ------ | ----------------------------------------- | -------------------------------------------- |
| Vinden | `Find` → `Advice` met `GetDeclaredSymbol` | Interfaces uit namespace `MediatR*` per type |
| Melden | `Rewrite` → `Advice`                      | `Manual` met de adviestekst                  |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `Advice`.

<!-- kingmadoc:diagram img/mediatr-unsupported-modellen-3.d2 -->
![Figuur 3: beslisboom](img/mediatr-unsupported-modellen-3.png)

| Blad                               | Voorwaarde                                             | Bewijs                   |
| ---------------------------------- | ------------------------------------------------------ | ------------------------ |
| Manual `MessagePreProcessor`       | `IRequestPreProcessor`                                 | `pre-processor`          |
| Manual `MessagePostProcessor`      | `IRequestPostProcessor`                                | `post-processor`         |
| Manual `MessageExceptionHandler`   | `IRequestExceptionHandler` / `IRequestExceptionAction` | `exception-handler`      |
| Manual `NotificationPublisherType` | `INotificationPublisher`                               | `notification-publisher` |
| Manual `StreamPipelineBehaviors`   | `IStreamPipelineBehavior`                              | `stream-behavior`        |
| Manual generieke berichten         | generiek type + bericht of request-handler             | `generic-handler`        |
| niet gevonden                      | anders                                                 | n.v.t.                   |

## Zie ook

- [mediatr-unsupported-plan.md](mediatr-unsupported-plan.md)
- [mediatr-unsupported-verify.md](mediatr-unsupported-verify.md)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`
- `src/Unshackle.Tests/Proofs/MediatR/Unsupported/`
