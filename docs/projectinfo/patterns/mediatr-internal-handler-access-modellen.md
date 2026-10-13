# Modellen: mediatr.internal-handler-access

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `InternalHandlerAccessPattern` | `ISolutionPattern` + `ISolutionFinder`; zoekt en schrijft alleen met de solution |
| `ISolutionFinder` | `FindAsync(root, model, solution, ct)`; de `Fixer` roept die aan in plaats van `Find` |
| `AddMediatRPattern` | `Find` (de `AddMediatR`-aanroepen), `ScannedAssemblies`, `IsHiddenHandler`, `Types`, `CanGrantAccess` (gedeeld) |
| `PatternResult.Fixed` | de hele `CompilationUnitSyntax` met de extra attribuutlijsten |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `FindAsync` → `IsChosenFile` | Is dit bestand het eerste (pad, positie) met een niet-publieke handler van deze assembly? |
| Hosts | `HostsAsync` | Per ander C#-project: documenten met de tekst `AddMediatR`, `AddMediatRPattern.Find`, `ScannedAssemblies` bevat deze assembly, en `GivesAccessTo` nog niet |
| Schrijven | `RewriteAsync` | Per host `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Host")]` vóór het eerste member (neemt diens leading trivia over) of na bestaande attributen |
| Host-kant | `AddMediatRPattern.RewriteAsync` → `HiddenHandler(..., CanGrantAccess)` | Assembly is een project van de solution zonder strong name: geen Manual |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Geen bevinding | strong-named, geen host, host heeft al toegang, of niet het gekozen bestand | `InternalHandlerAccessPatternTests` (`AlreadyGrants`, `NoOtherProjectScans`) |
| Fixed, één attribuut | één host scant | `_Combined/add-mediatr-internal-handlers-other-project` |
| Fixed, meerdere attributen | twee hosts | `..._TwoHostsScanInternalHandlers_...` |
| Manual (`Rewrite` zonder solution) | alleen buiten de Fixer | geen proof |

## Zie ook

- [mediatr-internal-handler-access-plan.md](mediatr-internal-handler-access-plan.md)
- [mediatr-internal-handler-access-verify.md](mediatr-internal-handler-access-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/_Combined/`
