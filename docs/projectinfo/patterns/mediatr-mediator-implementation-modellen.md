# Modellen: mediatr.mediator-implementation

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/MediatorImplementationPattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `MediatorImplementationPattern` | `IPattern`; vindt de klasse, herschrijft haar in één keer |
| `MediatRSyntax.IsMediatR` / `IsTaskMember` | Herkent MediatR-interfaces en `Task.CompletedTask`/`Task.FromResult` |
| `MediatRFix.SwappedTypes` | Swapt `IMediator`, `IRequest<T>`, `INotification`, `IStreamRequest<T>` via de using |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `Find` | Klassen met MediatR `IMediator`/`ISender`/`IPublisher` in de eigen basislijst |
| Members | `Rewrite` | Per interface-methode `FindImplementationForInterfaceMember`: in deze declaratie, niet expliciet, `IsNoOp` |
| Schrijven | `Members` | Mediator's members als tekst, met inspringing en regeleinde van het bestand; eerste krijgt de trivia van de eerste oude member |
| Usings | `PatternResult.Fixed(..., namespaces)` | `System.Threading(.Tasks)`, bij `ISender` ook `System.Collections.Generic` en `System.Runtime.CompilerServices` |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Manual "implemented in a base class" | member niet in deze declaratie | — |
| Manual "implements … explicitly" | expliciete implementatie | — |
| Manual "does more than complete" | logica in een member | `with-logic` |
| Fixed: sender + publisher | `IMediator` | `no-op` |
| Fixed: alleen publisher | `IPublisher` | `publisher` |

## Zie ook

- [mediatr-mediator-implementation-plan.md](mediatr-mediator-implementation-plan.md)
- [mediatr-mediator-implementation-verify.md](mediatr-mediator-implementation-verify.md)
