---
kingmadoc: 1
feature: "end-to-end-keten"
status: implemented
requirements:
    [
        REQ-1,
        REQ-2,
        REQ-3,
        REQ-4,
        REQ-5,
        REQ-6,
        REQ-7,
        REQ-8,
        REQ-9,
        REQ-10,
        REQ-11,
        REQ-12,
    ]
files_expected:
    - src/Unshackle.Core/Abstractions/ISolutionStore.cs
    - src/Unshackle.Core/Abstractions/IReportWriter.cs
    - src/Unshackle.Core/Abstractions/ISourceControl.cs
    - src/Unshackle.Core/Abstractions/SolutionLoadException.cs
    - src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs
    - src/Unshackle.DataAccess/SourceControl/GitSourceControl.cs
    - src/Unshackle.DataAccess/Reports/
    - src/Unshackle.Interface/Commands/
    - src/Unshackle.Interface/Output/
    - src/Unshackle.Interface/Cli.cs
    - src/Unshackle.Interface/Program.cs
    - src/Unshackle.Interface/Unshackle.Interface.csproj
    - src/Unshackle.Tests/DataAccess/
    - src/Unshackle.Tests/Interface/
    - src/Unshackle.Tests/Integration/
    - README.md
---

# Feature: End-to-end keten: unshackle scan, fix en list als dotnet tool

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Implemented               |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-005. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

`unshackle scan|fix|list` werkt als `dotnet tool` vanuit elke shell (PowerShell, bash) op een echte `.sln`/`.slnx`,
met rapport (CSV/JSON/Markdown), `--dry-run`-diff en vaste exitcodes.

## Scope (in / out)

**In scope**

- Solution laden en wegschrijven via één `MSBuildWorkspace` (`ISolutionStore` → `MsBuildSolutionStore`).
- Rapport-writers CSV, JSON en Markdown (`IReportWriter`), gekozen op de extensie van `--report`.
- Commands `list`, `scan`, `fix` (`--dry-run`, `--force`), console-uitvoer via Spectre.
- Veiligheidscheck: `fix` weigert op een git-werkmap met niet-gecommitte wijzigingen (`ISourceControl` → git).
- Installeerbaar als `dotnet tool` (`dotnet pack` + `dotnet tool install`), README "Usage" voor PowerShell en bash.
- Integratietest op een kopie van `samples/` in een tempmap (trait `Category=Integration`).

**Out of scope**

- Projectbestanden migreren (FA-pakket eruit, Shouldly erin, `<Using Include>` aanpassen): vervolgtaak.
- De gefixte sample bouwen en testen (kan pas na projectbestand-migratie).
- Een diff-pakket (DiffPlex): eerst eigen regel-diff.

## Requirements

- **REQ-1**: WHEN de gebruiker `unshackle list` draait THE SYSTEM SHALL elke fix tonen met per pattern het `Id` en de `Description`, en exitcode 0 geven.
- **REQ-2**: WHEN de gebruiker `unshackle scan <sln> --fix <id>` draait THE SYSTEM SHALL de solution laden, de fix draaien zonder iets te schrijven en per pattern het aantal Fixed en Manual tonen.
- **REQ-3**: WHEN `--report <pad>` met extensie `.csv`, `.json` of `.md` is meegegeven THE SYSTEM SHALL het rapport met kolommen `File,Line,Fix,Pattern,Status,Reason` naar dat pad schrijven, met paden relatief aan de map van de solution.
- **REQ-4**: WHEN de gebruiker `unshackle fix <sln> --fix <id>` draait op een schone werkmap THE SYSTEM SHALL de gewijzigde documenten wegschrijven.
- **REQ-5**: WHEN `fix` draait zonder `--dry-run` en zonder `--force` en de map van de solution in een git-werkmap met niet-gecommitte wijzigingen ligt THE SYSTEM SHALL weigeren, niets schrijven en exitcode 1 geven.
- **REQ-6**: WHEN `fix --dry-run` draait THE SYSTEM SHALL per gewijzigd document een regel-diff tonen en niets schrijven, ook op een vuile werkmap.
- **REQ-7**: WHEN de solution niet bestaat of MSBuild laadfouten meldt THE SYSTEM SHALL de fouten op stderr tonen en exitcode 1 geven.
- **REQ-8**: WHEN argumenten ongeldig zijn (onbekend command, ontbrekende `--fix`, onbekende fix-id, onbekende rapportextensie) THE SYSTEM SHALL een melding geven en exitcode 2 geven.
- **REQ-9**: WHEN scan of fix Manual-findings oplevert THE SYSTEM SHALL toch exitcode 0 geven (Manual is een uitkomst, geen fout).
- **REQ-10**: WHEN een pad spaties bevat THE SYSTEM SHALL het gewoon laden en schrijven.
- **REQ-11**: WHEN `dotnet pack src/Unshackle.Interface` en `dotnet tool install` zijn gedraaid THE SYSTEM SHALL als commando `unshackle` beschikbaar zijn.
- **REQ-12**: WHEN de integratietest draait THE SYSTEM SHALL op een kopie van `samples/` scan en fix doen en het rapport en een paar herschreven regels controleren, ook in CI (ubuntu).

## Assumptions

- _(inferred)_ Built on the existing stack: C#, .NET 10, Roslyn 5.9 `MSBuildWorkspace` met eigen BuildHost.
- De gebruiker heeft de solution gerestored (`obj/project.assets.json`), anders kent het SemanticModel de bibliotheek niet.
- `git` staat op het PATH; zonder git (of buiten een git-werkmap) geldt de map als schoon.

## Risks

- MSBuildWorkspace laadt niet (SDK, BuildHost) lokaal, in CI of vanuit de tool-installatie → laadfouten worden exitcode 1; integratietest en een echte `dotnet tool install` maken het zichtbaar.
- `fix` overschrijft gebruikerscode → standaard weigeren op een vuile werkmap, `--dry-run` eerst.
- System.CommandLine geeft standaard exitcode 1 bij parsefouten → zelf afvangen en 2 teruggeven.
- Integratietest wijzigt `samples/` → altijd op een kopie (zonder `bin/`/`obj/`) in een tempmap, met restore.

## C4 Context (Mermaid)

Who uses the system and which external systems it depends on.

```mermaid
C4Context
    title System Context: Unshackle

    Person(user, "Developer", "Draait unshackle vanuit PowerShell of bash")
    System(unshackle, "Unshackle", "dotnet tool: scan, fix, list")
    System_Ext(solution, "Solution van de gebruiker", ".sln/.slnx op schijf")
    System_Ext(msbuild, "MSBuild / .NET SDK", "Laadt projecten en referenties")
    System_Ext(git, "git", "Is de werkmap schoon?")

    Rel(user, unshackle, "Draait")
    Rel(unshackle, solution, "Leest en schrijft")
    Rel(unshackle, msbuild, "Laadt via")
    Rel(unshackle, git, "Vraagt status")
```

## C4 Container (Mermaid)

```mermaid
C4Container
    title Containers: Unshackle

    Person(user, "Developer", "PowerShell of bash")
    System_Boundary(unshackle_boundary, "Unshackle") {
        Container(unshackle_interface, "Unshackle.Interface", "C#, System.CommandLine, Spectre", "Commands scan/fix/list, console, composition root")
        Container(unshackle_core, "Unshackle.Core", "C#, Roslyn", "Fixer, fixes, patterns, contracten")
        Container(unshackle_dataaccess, "Unshackle.DataAccess", "C#, MSBuildWorkspace", "Solutions laden/opslaan, rapporten, git-status")
    }

    Rel(user, unshackle_interface, "unshackle ...")
    Rel(unshackle_interface, unshackle_core, "IFixer")
    Rel(unshackle_interface, unshackle_dataaccess, "Maakt aan")
    Rel(unshackle_dataaccess, unshackle_core, "Implementeert ISolutionStore, IReportWriter, ISourceControl")
```

## Open questions

- [x] `fix` op een vuile werkmap? Voorlopige keuze: weigeren tenzij `--force`; `--dry-run` mag altijd.
- [ ] Projectbestanden migreren (pakket en global usings): vervolgtaak.
