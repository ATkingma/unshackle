# Modellen: mediatr.request-processor-caller

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorCallerPattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `RequestProcessorCallerPattern` | `IPattern`; vindt processorverwijzingen en `Process`-aanroepen en schrijft ze om |
| `MediatRSyntax` | `ResponseOf` (gedeeld met `RequestProcessorPattern`), `DeclaredInterface`, `IsMediatR`, `CommaList` |
| `CallAnalysis` | Uitkomst van de analyse van één `Process`: probleem, pre of post, responsetype |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `Find` | `GenericName` van een pre-processorklasse met open bericht (buiten eigen klasse en `AddMediatR`), `Process`-naam en bijbehorende `ArgumentList` |
| Verwijzing | `RewriteReference` | `MediatRSyntax.ResponseOf` van het typeargument, tweede argument toevoegen |
| Aanroep | `Analyze` | Is het een `await x.Process(...);`-statement met positionele argumenten? |
| Naam | `Rewrite` (IdentifierName) | `Process` wordt `Handle` |
| Argumenten | `RewriteArguments` | `next`-delegate tussen request en token |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Fixed | `P<Cmd>` met `Cmd : IRequest<T>` of `IRequest` | `_Combined/request-processor-callers` |
| Fixed | `await p.Process(...)` pre of post | `_Combined/request-processor-callers` |
| Manual "unknown" | typeargument zonder `IRequest`, open argument | `unknown-response` |
| Manual "not a plain await statement" | resultaat gebruikt, niet awaited, mock, methodegroep | `result-used` |
| Manual "through the MediatR interface" | ontvanger is `IRequestPreProcessor<T>`/`IRequestPostProcessor<T,R>` | `through-interface` |

## Zie ook

- [mediatr-request-processor-caller-plan.md](mediatr-request-processor-caller-plan.md)
- [mediatr-request-processor-caller-verify.md](mediatr-request-processor-caller-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/RequestProcessorCaller/`
