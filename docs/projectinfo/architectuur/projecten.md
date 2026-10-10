# Projecten en afhankelijkheden

## Projecten

```
src/
├─ Unshackle.Interface/     ← waar de gebruiker mee werkt (CLI: fix, scan, list), console-output
├─ Unshackle.Core/          ← hoofdproject: de Fixer, fixes per bibliotheek, patronen per fix, contracten
├─ Unshackle.DataAccess/    ← alles wat leest of schrijft: solutions (MSBuild), rapporten (CSV/JSON/Markdown)
└─ Unshackle.Tests/         ← alle tests: unit, proofs per patroon, Integration/ (samples/)
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

## Build-instellingen

- .NET 10 (`global.json`), solution in `.slnx`
- `Directory.Build.props`: nullable aan, warnings tellen als errors, analyzers op `latest-recommended`
- `Directory.Packages.props`: alle pakketversies op één plek
- Interface wordt gepubliceerd als `dotnet tool` met het commando `unshackle`

## Zie ook

- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
- `docs/projectinfo/architectuur/core.md` — binnen Core
