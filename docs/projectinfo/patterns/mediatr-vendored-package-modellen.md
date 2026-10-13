# Modellen: mediatr.vendored-package

Feiten uit `src/Unshackle.Core/VendoredPackages.cs`, `VendoredPlan.cs` en `Fixes/MediatR/ArdalisSharedKernel.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `IFix.VendoredPackages` | Pakketten die de fix vendort; default leeg, `MediatRFix` geeft `[ArdalisSharedKernel.Package]` |
| `VendoredPackage` / `VendoredVersion` / `VendoredFile` | Naam, map, header, extra bestanden (licentie); per versie de bestanden en afhankelijkheden |
| `ArdalisSharedKernel` | Leest de embedded broncode (`Vendored/ArdalisSharedKernel/common` + familie `1.4.0`, `1.5.0`, `2.1.0`) en de nuspec-afhankelijkheden |
| `VendoredPackages` | Kiest versie en root-project, maakt documenten, nieuwe bestanden, migraties en rijen (`vendored-package`) |
| `VendoredPlan` | Uitkomst; `AddTo` zet de documenten in de opgeloste solution |
| `ProjectFileChange.Creates` | Nieuw bestand; `XmlProjectFileStore.SaveAsync` controleert dat het niet bestaat en draait terug |
| `UnsupportedPattern.IsVendored` | Geen Manual voor een ondersteunde versie |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Patronen | `Fixer.FixAsync` | Gebruikerscode wordt gemigreerd; `UnsupportedPattern` slaat Ardalis.SharedKernel over |
| Plan | `VendoredPackages.PlanAsync` | Gebruikers = projecten met de assembly; versie uit `AssemblyIdentity`; root via `ProjectDependencyGraph` |
| Bron | `PlanAsync` | Header + resource, regeleinde van het project; document per target van het root-project |
| Afhankelijkheden | `Resolved` | Versie uit het NuGet-pad van de referentie, anders nuspec |
| Projectbestanden | `Fixer.MigrateProjectFilesAsync` | MediatR-migraties en vendor-migraties in één `IProjectFileStore.EditAsync` |
| Schrijven | `FixCommand` → `SaveAsync` | Nieuwe bestanden met de rest, in één controle vooraf |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Fixed (bron + swap) | ondersteunde versie, één root, map vrij | `_Combined/Vendored/shared-kernel`, `FixAsync_UsersShareOneProject_*` |
| Manual "no single project for its source" | gebruikers zonder gemeenschappelijk project | `FixAsync_UsersShareNoProject_ReportsManual` |
| Manual "no source of this version" | 1.0.0–1.3.0 (ook `UnsupportedPattern`) | code-review (`PlanAsync`) |
| Manual "already has source files" | map bestaat in de solution | code-review (`PlanAsync`) |
| Fout bij schrijven "already exists" | bestand staat al op schijf | `SaveAsync_NewFileAlreadyThere_*` |

## Zie ook

- [mediatr-vendored-package-plan.md](mediatr-vendored-package-plan.md)
- [mediatr-vendored-package-verify.md](mediatr-vendored-package-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/_Combined/Vendored/`
