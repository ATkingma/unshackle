# TAAK-005 — End-to-end keten: laden, CLI, rapport, eerste integratietest

**Status**: stap 1/8 — nog te starten
**Aangemaakt**: 2026-10-10
**Doel**: `unshackle scan|fix|list` werkt op een echte solution (`samples/SampleShop`), met rapport en `--dry-run`-diff, bewezen door een integratietest.

## Context voor de volgende agent

- Voortgang 2026-10-10: ~15–20% van de roadmap. Fundament (TAAK-001) en 4 FA-patronen (TAAK-002..004) zijn klaar,
  maar de tool is **niet bruikbaar**: `src/Unshackle.Interface/Program.cs` is `return 0;`, `src/Unshackle.DataAccess/`
  heeft alleen lege mappen `Solutions/` en `Reports/`, `src/Unshackle.Tests/Integration/` is leeg.
- Bestaand contract: `IFixer.FixAsync(Solution, FixRequest, CancellationToken)` → `FixReport { Solution, Findings }`;
  `Finding { FilePath, Line, FixId, PatternId, Result }`; `Fixer(IEnumerable<IFix>)`. Contracten in `Abstractions/`
  mogen **uitgebreid** worden met nieuwe interfaces (`ISolutionStore`, `IReportWriter`), bestaande niet wijzigen.
- Pakketten staan al: DataAccess `Microsoft.CodeAnalysis.Workspaces.MSBuild`, `System.IO.Abstractions`, `CsvHelper`;
  Interface `System.CommandLine`, `Spectre.Console`. Geen `Microsoft.Build.Locator` (Roslyn 5.x heeft een eigen BuildHost).
- Patroon-loop staat stil (gestopt 2026-10-10 23:50); rij 4–14 in `claude/tasks/patroon-queue.md` zijn nog open.
  Deze taak gaat vóór de loop: zonder keten kun je patronen niet op echte code zien werken.

## Analyse

### Huidige situatie

Patronen werken alleen in proofs (in-memory `AdhocWorkspace` via `src/Unshackle.Tests/Helpers/TestSolution.cs`).
Er is geen manier om een `.sln/.slnx` te laden, wijzigingen weg te schrijven, een rapport te maken of de tool te starten.

### Probleem

- Niemand kan Unshackle draaien; roadmap-stap 1 (scan + rapport) is niet af.
- Of patronen samen op echte code werken (MSBuild, global usings uit csproj, meerdere projecten) is onbewezen.

### Nieuwe situatie

`unshackle scan <sln> --fix fluentassertions [--report r.csv]`, `unshackle fix <sln> --fix … [--dry-run]` en
`unshackle list` werken. Een integratietest draait de keten op een kopie van `samples/SampleShop`.

### Verbetering

Eerste bruikbare versie; elk volgend patroon is direct op echte code te zien (scan op SampleShop).

## Aanpak

### Wat

1. `ISolutionStore` (Core) → `MsBuildSolutionStore` (DataAccess): `LoadAsync(path, ct)` en `SaveAsync(Solution, ct)`
   via één `MSBuildWorkspace` (`TryApplyChanges`); laadfouten (`workspace.Diagnostics`/`WorkspaceFailed`) teruggeven.
2. `IReportWriter` (Core) → `CsvReportWriter`, `JsonReportWriter`, `MarkdownReportWriter` (DataAccess, `IFileSystem`).
   Kolommen: `File,Line,Fix,Pattern,Status,Reason` (zie `docs/projectinfo/architectuur/rapportage.md`).
3. Commands (Interface, `Commands/`): `scan`, `fix` (`--dry-run` toont diff per bestand), `list` (fixes + pattern-Id + Description).
   Console-uitvoer in `Output/` via Spectre (tabel met Fixed/Manual per pattern).
4. `Program.cs`: composition root met de hand (`new Fixer([new FluentAssertionsFix()])` enz.), geen DI-container.
5. Integratietest in `src/Unshackle.Tests/Integration/`: kopieer `samples/SampleShop` naar een tempmap, `scan` + `fix`,
   controleer rapport (aantallen Fixed/Manual) en een paar herschreven regels.

### Waar

`src/Unshackle.Core/Abstractions/ISolutionStore.cs`, `IReportWriter.cs`; `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`;
`src/Unshackle.DataAccess/Reports/{Csv,Json,Markdown}ReportWriter.cs`; `src/Unshackle.Interface/Commands/{Scan,Fix,List}Command.cs`;
`src/Unshackle.Interface/Output/*`; `src/Unshackle.Interface/Program.cs`; `src/Unshackle.Tests/Integration/SampleShopTests.cs`.

### Hoe

Eerst DataAccess + unittests (`MockFileSystem` voor writers), dan commands met gemockte `IFixer`/`IReportWriter`
(NSubstitute, zie `docs/projectinfo/architectuur/testen.md`), dan `Program.cs`, als laatste de integratietest.
KingmaDoc-plan vooraf (`docs/projectinfo/patterns/` is voor patronen; dit is een feature → plan in `docs/projectinfo/features/`
met `kingmadoc plan -o`), verify achteraf, en modellen bijwerken (Container/Component Interface + DataAccess).

### Waarom deze aanpak

Handmatige composition root: drie klassen, een DI-container is overbodig. Diff: eerst eenvoudig per regel zelf bepalen
(oude vs nieuwe tekst van gewijzigde documenten); alleen een pakket als DiffPlex toevoegen als dat te beperkt blijkt
(MIT, geen licentiecheck — wel even met de gebruiker afstemmen).

## Stappen

- [ ] 1. KingmaDoc-plan `docs/projectinfo/features/end-to-end-keten-plan.md` (requirements per command), check + approve
- [ ] 2. `ISolutionStore` + `MsBuildSolutionStore` (laden, opslaan, laadfouten) + test op `samples/SampleShop`
- [ ] 3. `IReportWriter` + CSV/JSON/Markdown writers + unittests (`MockFileSystem`)
- [ ] 4. Commands `list`, `scan`, `fix` (+ `--dry-run`-diff) + unittests met mocks
- [ ] 5. `Program.cs` composition root; handmatig `dotnet run --project src/Unshackle.Interface -- scan samples/SampleShop/SampleShop.slnx --fix fluentassertions`
- [ ] 6. Integratietest op kopie van SampleShop (trait `Category=Integration`, apart filterbaar)
- [ ] 7. KingmaDoc-verify, modellen bijwerken (Modellen-brief in `claude/docs/patroon-loop.md`), docs `rapportage.md` + `core.md`
- [ ] 8. Drie checks groen (`claude/docs/agent-werkwijze.md` §6), commit + push

## Risico's & randgevallen

| Risico                                                                                                                                                 | Mitigatie                                                                                                                             |
| ------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| MSBuildWorkspace laadt niet (SDK-versie, BuildHost) in CI of lokaal                                                                                    | Laadfouten tonen en als exitcode ≠ 0 teruggeven; integratietest draait in CI → meteen zichtbaar                                       |
| SampleShop gebruikt `<Using Include="FluentAssertions" />` en central package management: na `fix` staan FA-pakket en global using nog in csproj/props | Buiten scope: rapporteren als Manual op projectniveau; vervolgtaak "projectbestanden migreren" (pakket wisselen, `<Using>` aanpassen) |
| `fix` overschrijft gebruikerscode                                                                                                                      | Standaard eerst `scan`; `fix` alleen op een schone git-werkmap of met `--force` (beslissen in stap 1)                                 |
| Integratietest wijzigt `samples/`                                                                                                                      | Altijd op een kopie in een tempmap                                                                                                    |
| Gelijktijdige patroon-agents                                                                                                                           | Loop staat stil; pas herstarten na deze taak                                                                                          |

## Gewijzigde bestanden

_(wordt aangevuld tijdens uitvoering)_

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `docs/projectinfo/architectuur/projecten.md` — afhankelijkheden Interface → Core ← DataAccess
- `docs/projectinfo/architectuur/rapportage.md` — rapportformaat
- `claude/tasks/archief/TAAK-001-fundament.md` — contract en keuzes
