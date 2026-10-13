# Modellen: mediatr.generic-request

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericRequestPattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `GenericRequestPattern` | `ISolutionPattern` + `ISolutionFinder`: vinden en de declaratie herschrijven met de compilaties van alle C#-projecten |
| `IsCandidate` (statisch) | Generiek request, geen stream-request; gebruikt door `UnsupportedPattern` |
| `Uses` (genest) | Uitkomst van `Scan`: gesloten typeargumenten, bestaande subklassen, eerste `typeof(G<>)`, eerste open creatie |
| `GenericNotificationPattern` | Doet generieke notifications met gesloten gebruik; dit patroon alleen de notification die enkel basis is |
| `FixedResult` (met `Warning`) / `ManualResult` | Uitkomst |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `FindNodes` | Kandidaat-declaratie, alleen-gesubklasseerde notification, concrete handler met geërfde `IRequestHandler<G<X, Y>, Y>` (`InheritedHandlers`), gesloten `G<X, Y>` (`Use`) |
| Solution | `FindAsync` / `RewriteAsync` | Compilaties van alle C#-projecten; gebruiksplekken alleen met het eigen model |
| Scannen | `Scan` | Elke `G<...>` in elke boom: gesloten, subklasse, unbound, open creatie |
| Declaratie | `RewriteNode` | Manual-checks, typen mappen naar het project van `G`, `MakeAbstract`, subklassen met doorgegeven constructors |
| Handler | `RewriteNode` (handler-tak) | `IRequestHandler<GOfXAndY, Y>` in de basislijst + brug-`Handle` naar `base.Handle` |
| Gebruik | `RewriteUse` | `G<X, Y>` → `GOfXAndY`, qualifier blijft |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Fixed: alleen `abstract` | geen gesloten gebruik, wel concrete subklassen | `Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration`, `_Combined/generic-decorators-kgrzybek` |
| Manual "not a plain" | typeargument geen gewoon benoemd type | `not-plain-argument` |
| Manual "record, partial or file-local" | zo gedeclareerd | `derived-generic-record` |
| Manual "becomes abstract" (typeof) | `typeof(G<>)` | `unbound-typeof` |
| Manual "open type arguments" | `new G<TParam>` in generieke code | `open-creation` |
| Manual "not visible from" | gesloten type in project buiten bereik van `G` | `RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual` |
| Fixed: abstract + subklassen + hernoemen | gesloten gebruik (ook in andere projecten) | `closed-uses`, `RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass` |
| Fixed: brug-`Handle` | concrete handler erft van abstracte generieke basis | `_Combined/identified-command-eshop` |

## Zie ook

- [mediatr-generic-request-plan.md](mediatr-generic-request-plan.md)
- [mediatr-generic-request-verify.md](mediatr-generic-request-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/GenericRequest/`
