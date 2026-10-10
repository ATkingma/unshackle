# Klassen: contracten en modellen van Unshackle.Core

**Figure 1.** Contracten (`Abstractions/`) en modellen (`Models/`) rond `Fixer`. Weggelaten: `FluentAssertionsFix` en de patterns (zie [patronen.md](patronen.md)) en `UsingDirectives`.

<!-- kingmadoc:diagram img/klassen-1.d2 -->
![Klassen: contracten en modellen van Unshackle.Core](img/klassen-1.png)

| Element | Pad | Rol |
| ------- | --- | --- |
| `IFixer` / `Fixer` | `Abstractions/IFixer.cs`, `Fixer.cs` | Enige ingang (Facade): draait één fix op een `Solution` |
| `IFix` | `Abstractions/IFix.cs` | Eén migratie (Strategy): patterns plus toe te voegen en te verwijderen usings |
| `IPattern` | `Abstractions/IPattern.cs` | Eén soort code vinden (`Find`) en herschrijven (`Rewrite`); gooit nooit |
| `FixRequest` | `Models/FixRequest.cs` | Welke fix (`FixId`) |
| `FixReport` | `Models/FixReport.cs` | Nieuwe `Solution` en alle bevindingen |
| `Finding` | `Models/Finding.cs` | Eén gevonden node: bestand, regel (0 = heel bestand), fix, pattern, resultaat |
| `PatternResult` | `Models/PatternResult.cs` | Fabrieksmethoden `Fixed` (één of meer nodes) en `Manual` |
| `FixedResult` | `Models/FixedResult.cs` | Vervangende nodes en benodigde namespaces |
| `ManualResult` | `Models/ManualResult.cs` | Reden waarom handwerk nodig is |

## Zie ook

- [fix-flow.md](fix-flow.md) — hoe `FixAsync` deze klassen gebruikt
- [../architectuur/core.md](../architectuur/core.md) — ontwerp en keuzes
- [../README.md](../README.md) — C4-overzicht
