# Modellen: mediatr.request-handler-delegate

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs`, `src/Unshackle.Core/Fixer.cs` en `src/Unshackle.Core/ProjectPackages.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `RequestHandlerDelegatePattern` | Vindt het type dat `RequestHandlerDelegate<T>` buiten een behavior-`Handle` noemt en herschrijft het type als geheel |
| `MediatRSyntax` | `IsMediatR`, `IsTaskMember`, `OnValueTask`, `CommaList` |
| `ProjectPackages.RemainingUsesAsync` | Vangnet: overgebleven MediatR-symbolen per project |
| `Fixer.UnmatchedPatternId` | Patroon-id `unmatched-use` van de vangnet-rijen |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `DelegateReferences`, `InnermostType` | Elke `RequestHandlerDelegate<T>`-naam (niet de parameter van een behavior-`Handle`); het bevattende type is het finding |
| Houder | `Holder` | Veld of lokale variabele: gedeclareerd type, of `Substitute.For<...>()` toegekend of geïnitialiseerd |
| Gebruik | `Rewrite` (switch) | Per gebruik: toewijzing van `Substitute.For`, `next`-argument van een behavior-`Handle` (levert TReq), of `Invoke()`-configuratie |
| Herschrijven | `ReplaceNodes` | Type, `Invoke`-argumenten, `Task.FromResult` in `Returns` |
| Vangnet | `Fixer.MigrateProjectFilesAsync` | Overgebleven symbolen in bestanden zonder Manual: Manual `unmatched-use` op die regel |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Fixed | mock + `Handle`-argument + `Invoke().Returns(x)` / `Received().Invoke()` | `_Combined/request-handler-delegate` |
| Manual "not a field or local" | parameter van een hulpmethode, property, publiek veld | `parameter` |
| Manual "initialized with a real delegate" | `RequestHandlerDelegate<int> next = () => ...` | `lambda` |
| Manual "used other than as a mock configuration" | `await next()` of `next.Invoke()` buiten `Returns` | `invoke-call` |
| Manual "is a Task or a callback" | `Returns(_ => ...)` of een andere Task | `callback-returns` |
| Manual "not handed to exactly one request type" | geen of verschillende `Handle`-aanroepen | `no-handle-call` |

## Zie ook

- [mediatr-request-handler-delegate-plan.md](mediatr-request-handler-delegate-plan.md)
- [mediatr-request-handler-delegate-verify.md](mediatr-request-handler-delegate-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/`
