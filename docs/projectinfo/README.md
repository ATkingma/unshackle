# Unshackle — projectinfo

Hoofddocument van de projectdocumentatie. Het overzicht en de C4-diagrammen staan hier; details staan in losse
documenten, en elk patroon heeft een eigen plan en verificatie (KingmaDoc) in `patterns/`.

## Documenten

| Document                                                 | Inhoud                                                               |
| -------------------------------------------------------- | -------------------------------------------------------------------- |
| [project.md](project.md)                                 | Idee, probleem, bestaande tools, CLI, voorbeelden, roadmap, risico's |
| [architectuur/projecten.md](architectuur/projecten.md)   | Projecten, afhankelijkheden, build-instellingen                      |
| [architectuur/core.md](architectuur/core.md)             | Fixer → Fix → Pattern, zoals ontworpen en zoals gebouwd, SOLID       |
| [architectuur/testen.md](architectuur/testen.md)         | Proofs, types zonder te builden, mocking, samples                    |
| [architectuur/rapportage.md](architectuur/rapportage.md) | Rapportformaten (nog niet gebouwd)                                   |
| [beslissingen.md](beslissingen.md)                       | Codestijl en wat nog beslist moet worden                             |

## C4 Context

Wie Unshackle gebruikt en waar het van afhangt.

```mermaid
C4Context
    title System Context: Unshackle

    Person(dev, "Developer", "Migreert een .NET-solution weg van commerciële bibliotheken")
    System(unshackle, "Unshackle", "dotnet tool: scan, fix, list")
    System_Ext(solution, "Solution van de gebruiker", ".sln/.slnx met C#-projecten op schijf")
    System_Ext(msbuild, "MSBuild / .NET SDK", "Laadt projecten en referenties")

    Rel(dev, unshackle, "Draait")
    Rel(unshackle, msbuild, "Laadt solution via")
    Rel(unshackle, solution, "Leest en herschrijft")
```

## C4 Container

De projecten in `src/` _(uit de code; Interface en DataAccess zijn nog leeg)_.

```mermaid
C4Container
    title Containers: Unshackle

    Person(dev, "Developer")
    System_Boundary(unshackle_boundary, "Unshackle") {
        Container(cli, "Unshackle.Interface", "C# / System.CommandLine", "CLI en composition root")
        Container(core, "Unshackle.Core", "C# / Roslyn", "Fixer, fixes en patterns")
        Container(data, "Unshackle.DataAccess", "C# / MSBuildWorkspace", "Solution laden/opslaan, rapporten schrijven")
    }

    Rel(dev, cli, "Uses")
    Rel(cli, core, "Roept Fixer aan")
    Rel(cli, data, "Koppelt in Program.cs")
    Rel(data, core, "Implementeert interfaces van")
```

## C4 Component: Unshackle.Core

_(uit de code, stand TAAK-002)_

```mermaid
C4Component
    title Components: Unshackle.Core

    Container_Boundary(core_boundary, "Unshackle.Core") {
        Component(fixer, "Fixer", "IFixer", "Draait patterns per document, vervangt nodes, regelt usings")
        Component(usings, "UsingDirectives", "C#", "Voegt usings toe en verwijdert ongebruikte")
        Component(fafix, "FluentAssertionsFix", "IFix", "Lijst FA-patterns, FA eruit, Shouldly erin")
        Component(fasyntax, "FluentAssertionsSyntax", "C#", "Herkent x.Should().Methode(...) via SemanticModel")
        Component(patterns, "Patterns", "IPattern", "Eén soort code vinden en herschrijven; zie tabel hieronder")
    }

    Rel(fixer, fafix, "Haalt patterns uit")
    Rel(fixer, patterns, "Find / Rewrite")
    Rel(fixer, usings, "Past usings aan via")
    Rel(patterns, fasyntax, "Gebruikt")
```

## Patronen

Elk patroon heeft een KingmaDoc-plan (vóór de bouw) en -verificatie (na de bouw) in `patterns/`.
Wachtrij en status: `claude/tasks/patroon-queue.md`.

| Patroon-ID      | Plan                                   | Verificatie                                | Taak     |
| --------------- | -------------------------------------- | ------------------------------------------ | -------- |
| `fa.should-be`  | [plan](patterns/fa-should-be-plan.md)  | [verify](patterns/fa-should-be-verify.md)  | TAAK-001 |
| `fa.have-count` | [plan](patterns/fa-have-count-plan.md) | [verify](patterns/fa-have-count-verify.md) | TAAK-002 |
| `fa.be-true-false` | [plan](patterns/fa-be-true-false-plan.md) | [verify](patterns/fa-be-true-false-verify.md) | TAAK-003 |

## Zie ook

- `claude/docs/INDEX.md` — index voor agents
- `claude/docs/patroon-loop.md` — hoe patronen gemaakt en gedocumenteerd worden
- `claude/docs/code-standaarden.md` — codestijl
