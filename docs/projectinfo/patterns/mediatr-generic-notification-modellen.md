# Modellen: mediatr.generic-notification

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `GenericNotificationPattern` | `ISolutionPattern`; declaratie met hele solution (`RewriteAsync`), gebruiksplekken lokaal |
| `ClosedTypes` (genest) | Uitkomst van `Scan`: gesloten typeargumenten, typen met de constraint, `MakeGenericType` ja/nee, eerste subklasse |
| `MediatRSyntax.IsMediatR` | Herkent MediatR `INotification` / requests |
| `FixedResult` (met `Warning`) / `ManualResult` | Uitkomst |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `Find` | Kandidaat-declaratie (`IsCandidate`), `new` met kandidaat-type, `typeof(G<>)` (als `MakeGenericType`-aanroep of los) buiten de kandidaat zelf |
| Solution | `RewriteAsync` | Compilaties van alle C#-projecten → `RewriteDeclaration` |
| Scannen | `Scan` | Elke `G<...>` in elke boom binden; `GetSymbolsWithName` voor subklassen en typen met de constraint |
| Schrijven | `RewriteDeclaration` | `MakeAbstract`, per type een subklasse (`Parameters` vervangt `T`), eventueel `GTypes.Close` |
| Gebruik | `RewriteUse` | `Retarget` houdt de qualifier (`N.G<X>` → `N.GOfX`) |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Fixed: abstract + subklassen + `Close` | `MakeGenericType` en `T : C` | `make-generic-type`, `_Combined/generic-notification-nadirbad` |
| Fixed: abstract + subklassen | alleen gesloten verwijzingen | `new-closed`, `last-in-file` |
| Fixed: `new GOfX(...)` | `new G<X>` / doeltype-`new` | `new-closed` |
| Manual "concrete notification" | `new G<TParam>`, nooit gesloten | `generic-code` |
| Manual "no class or interface constraint" | `MakeGenericType` zonder constraint | `no-constraint` |
| Manual "not visible from" / "already has a subclass" | projectgrens, bestaande subklasse | `GenericNotificationPatternTests` |

## Zie ook

- [mediatr-generic-notification-plan.md](mediatr-generic-notification-plan.md)
- [mediatr-generic-notification-verify.md](mediatr-generic-notification-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/GenericNotification/`
