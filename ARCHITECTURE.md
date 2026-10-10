# Architectuur

## Projecten
```
src/
├─ Unshackle.Interface/     ← waar de gebruiker mee werkt (CLI: fix, scan, list), console-output
├─ Unshackle.Core/          ← hoofdproject: de Fixer, fixes per bibliotheek, patronen per fix, contracten
└─ Unshackle.DataAccess/    ← alles wat leest of schrijft: solutions (MSBuild), rapporten (CSV/JSON/Markdown)
tests/
├─ Unshackle.Tests/         ← unittests + proofs per patroon
└─ Unshackle.IntegrationTests/  ← draait de tool op echte projecten in samples/
```

## Afhankelijkheden
```
Interface ──► Core ◄── DataAccess
    └─────────────────────►┘
```
- **Core kent geen ander project.** Wat Core van buiten nodig heeft, legt Core vast als
  interface (`ISolutionStore`, `IReportWriter`).
- **DataAccess implementeert die interfaces.**
- **Interface koppelt alles** in `Program.cs` (composition root). Een tweede interface, zoals
  web of een VS Code-extensie, kan later zonder Core of DataAccess aan te passen.

## Binnen Core: Fixer → Fix → Pattern
```
Fixer                       de ene ingang: FixAsync(solution, request)
 └─ IFix                    één per bibliotheek (FluentAssertions, MediatR, AutoMapper)
     └─ IPattern            één soort code vinden + herschrijven
          ├─ Find(...)      → plekken in de code
          └─ Rewrite(...)   → Fixed(nieuwe node) of Manual(reden)
```
```
src/Unshackle.Core/
├─ Fixer.cs
├─ Abstractions/   IFix, IPattern, IFixer, ISolutionStore, IReportWriter
├─ Models/         Finding, PatternResult, FixRequest, FixReport
└─ Fixes/
   ├─ FluentAssertions/   FluentAssertionsFix.cs + Patterns/
   ├─ MediatR/            MediatRFix.cs + Patterns/
   └─ AutoMapper/         AutoMapperFix.cs + Patterns/
```
Elke fix heeft dezelfde indeling. Ken je er één, dan ken je ze allemaal.

**Afwijkende patronen** (bijvoorbeeld `HaveCount`, waar het type bepaalt wat de juiste
vervanging is) regel je binnen dat pattern. Wordt het te groot, dan splits je het in meerdere
patterns, zoals `HaveCountOnListPattern` en `HaveCountOnEnumerablePattern`. De Fixer merkt
daar niks van.

**Een pattern crasht nooit.** Lukt een herschrijving niet, dan geeft het `Manual(reden)`
terug en komt dat in het rapport.

**Dry-run kost niks extra.** Roslyn past een `Solution` nooit aan. De Fixer berekent een
nieuwe `Solution` en het rapport. Bij `--dry-run` tonen we alleen de diff, anders schrijft
`ISolutionStore` de wijzigingen weg.

## SOLID in het kort
| Principe | Hoe |
|---|---|
| S | Een pattern beschrijft alleen wat er herschreven wordt. Laden, schrijven en rapporteren gebeurt elders |
| O | Een nieuwe bibliotheek of nieuw patroon is een nieuwe klasse. De Fixer verandert niet |
| L | Elk pattern geeft `Fixed` of `Manual` terug en gooit nooit een exception, dus de Fixer behandelt ze allemaal hetzelfde |
| I | Kleine interfaces: zoeken/herschrijven, solution-opslag en rapport-schrijven zijn apart |
| D | Core hangt af van interfaces. MSBuild, het bestandssysteem en CSV zitten in DataAccess |

## Rapportage
Elk formaat is een eigen `IReportWriter` in DataAccess (`CsvReportWriter`, `JsonReportWriter`,
`MarkdownReportWriter`). Interface kiest er een op basis van de extensie van `--report`.
```csv
File,Line,Fix,Pattern,Status,Reason
tests/UserTests.cs,14,fluentassertions,fa.should-be,Fixed,
tests/OrderTests.cs,8,fluentassertions,fa.have-count,Manual,Dictionary: controleer of Count of Keys bedoeld is
```

## Testen

### Proofs: elk patroon testen zonder nieuwe testcode
```
tests/Unshackle.Tests/Proofs/<Fix>/<Pattern>/
   simple.before.cs      simple.after.cs
   dictionary.before.cs  dictionary.manual.txt    ← verwacht: handwerk
```
Eén test (`ProofTests`) vindt alle proofs zelf en draait de Fixer erop. Een nieuw patroon of
afwijkend geval testen betekent twee bestanden toevoegen. De map `Proofs/` wordt niet
meegecompileerd: het is testdata.

**Veiligheidscheck in elke proof:** de compilatie van `before` moet foutloos zijn met de oude
bibliotheek, en die van `after` met de nieuwe. Zo vang je een vergeten referentie af (anders
geeft het `SemanticModel` stilletjes error types terug) en een rewrite die niet compileert.

### Types zonder te builden
Unittests gebruiken een `Solution` in het geheugen (`AdhocWorkspace`). De types komen uit:
1. **Echte assemblies** (standaard). Het testproject verwijst naar de laatste vrije versies
   (FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0, plus Shouldly en Mediator.Abstractions).
   .NET zelf komt uit `Basic.Reference.Assemblies.Net100`. Een helper `TestReferences` geeft per
   fix de juiste set `MetadataReference`s.
2. **Stubs**: alleen voor uitzonderingen die je niet met het echte pakket kunt namaken.
3. **Echte sample-projecten** in `samples/`, die in `Unshackle.IntegrationTests` met MSBuild
   worden geladen, gefixt, gebouwd en getest. Traag, dus weinig, en in een aparte CI-job.

### Mocking (NSubstitute)
Mock alleen je **eigen** interfaces, nooit Roslyn-typen. Voor Roslyn gebruik je een echte
in-memory solution.

| Wat test je | Hoe |
|---|---|
| Een pattern | Proofs met echte assemblies |
| De Fixer-logica | Gemockte `IFix` en `IPattern`, kleine in-memory solution |
| Commands | Gemockte `IFixer`, `IReportWriter` en `IConsoleView` |
| Report writers | `MockFileSystem` (System.IO.Abstractions), geen echte bestanden |
| End-to-end | `samples/` in de integratietests |

Moq gebruiken we bewust niet (SponsorLink-ophef in 2023, en het is zelf een migratiekandidaat).

## Build-instellingen
- .NET 10 (`global.json`), solution in `.slnx`
- `Directory.Build.props`: nullable aan, warnings tellen als errors, analyzers op `latest-recommended`
- `Directory.Packages.props`: alle pakketversies op één plek
- Interface wordt gepubliceerd als `dotnet tool` met het commando `unshackle`

## Nog te beslissen
- Code-structuur en codestijl: naamgeving, wanneer een record en wanneer een class,
  async-conventies, foutafhandeling, regels in `.editorconfig`
- Patroon-ID's: formaat (`fa.should-be`?)
- CLI-opties: definitieve namen en standaardwaarden
- CI-workflows in `.github/workflows/`
