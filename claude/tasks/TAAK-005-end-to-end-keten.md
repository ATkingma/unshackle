# TAAK-005 — End-to-end keten: laden, CLI, rapport, eerste integratietest

**Status**: stap 8/8 — in uitvoering (checks + commit/push door hoofdsessie)
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

- 2026-10-11: stap 1–7 klaar (subagent). Volgende: stap 8 — drie checks op de commit zelf, dan commit + push (hoofdsessie). Open: projectbestanden migreren (vervolgtaak); CI-run van de integratietest nog niet gezien.
- 2026-10-11: overgenomen door een subagent (Claude Opus 5.5) vanuit de hoofdsessie; stap 1–7, stap 8 doet de hoofdsessie.

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

- [x] 1. KingmaDoc-plan `docs/projectinfo/features/end-to-end-keten-plan.md` (requirements per command), check + approve
- [x] 2. `ISolutionStore` + `MsBuildSolutionStore` (laden, opslaan, laadfouten) + test op `samples/SampleShop`
- [x] 3. `IReportWriter` + CSV/JSON/Markdown writers + unittests (`MockFileSystem`)
- [x] 4. Commands `list`, `scan`, `fix` (+ `--dry-run`-diff) + unittests met mocks
- [x] 5. `Program.cs` composition root; handmatig `dotnet run --project src/Unshackle.Interface -- scan samples/SampleShop/SampleShop.slnx --fix fluentassertions`
- [x] 6. Integratietest op kopie van SampleShop (trait `Category=Integration`, apart filterbaar)
- [x] 7. KingmaDoc-verify, modellen bijwerken (Modellen-brief in `claude/docs/patroon-loop.md`), docs `rapportage.md` + `core.md`
- [→] 8. Drie checks groen (`claude/docs/agent-werkwijze.md` §6), commit + push

## Risico's & randgevallen

| Risico                                                                                                                                                 | Mitigatie                                                                                                                             |
| ------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| MSBuildWorkspace laadt niet (SDK-versie, BuildHost) in CI of lokaal                                                                                    | Laadfouten tonen en als exitcode ≠ 0 teruggeven; integratietest draait in CI → meteen zichtbaar                                       |
| SampleShop gebruikt `<Using Include="FluentAssertions" />` en central package management: na `fix` staan FA-pakket en global using nog in csproj/props | Buiten scope: rapporteren als Manual op projectniveau; vervolgtaak "projectbestanden migreren" (pakket wisselen, `<Using>` aanpassen) |
| `fix` overschrijft gebruikerscode                                                                                                                      | Standaard eerst `scan`; `fix` alleen op een schone git-werkmap of met `--force` (beslissen in stap 1)                                 |
| Integratietest wijzigt `samples/`                                                                                                                      | Altijd op een kopie in een tempmap                                                                                                    |
| Gelijktijdige patroon-agents                                                                                                                           | Loop staat stil; pas herstarten na deze taak                                                                                          |

## Gewijzigde bestanden

- Core: `Abstractions/{ISolutionStore,IReportWriter,ISourceControl,SolutionLoadException}.cs` (nieuw; bestaande contracten ongewijzigd)
- DataAccess: `Solutions/MsBuildSolutionStore.cs`, `Reports/{ReportRow,CsvReportWriter,JsonReportWriter,MarkdownReportWriter}.cs`,
  `SourceControl/GitSourceControl.cs` (`.gitkeep` in `Solutions/`, `Reports/` weg)
- Interface: `Cli.cs`, `Program.cs`, `Commands/{ScanCommand,FixCommand,ListCommand,ScanResult,ExitCodes,CommandException}.cs`,
  `Output/{IConsoleView,SpectreConsoleView,LineDiff,DiffLine,DiffKind}.cs`, `Unshackle.Interface.csproj` (`<Version>0.1.0-preview</Version>`)
- Tests: `DataAccess/{MsBuildSolutionStoreTests,GitSourceControlTests}.cs`, `DataAccess/Reports/ReportWriterTests.cs`,
  `Interface/CliTests.cs`, `Interface/Commands/CommandTests.cs`, `Interface/Output/LineDiffTests.cs`,
  `Integration/SampleShopTests.cs`, `Helpers/{TempDirectory,RepositoryRoot}.cs` (`.gitkeep`s weg)
- `.editorconfig` (CA1716 uit voor Interface-code en -tests)
- Docs: `docs/projectinfo/features/end-to-end-keten-{plan,verify}.md`, `docs/projectinfo/README.md` (Documenten, C4 1/2, nieuwe
  Figure 4), `docs/projectinfo/img/README-2.*`, `README-4.*`, `docs/projectinfo/modellen/klassen.md` + `img/klassen-2.*`,
  `architectuur/{core,rapportage}.md`, `claude/docs/INDEX.md`, `README.md` (sectie Usage)

## Keuzes

| Keuze | Reden |
| --- | --- |
| `fix` weigert op een git-werkmap met niet-gecommitte (ook untracked) wijzigingen, tenzij `--force`; `--dry-run` altijd. **Voorlopig** | Veilige standaard: alleen op een schone werkmap is `git checkout .` een volledige undo. Hele werkmap i.p.v. alleen de solution-map: projecten kunnen buiten die map liggen |
| Geen git of geen repo → telt als schoon | Anders werkt de tool niet op mappen zonder git (en niet in de tempmap van de integratietest); de gebruiker ziet nog steeds de scan |
| Exitcodes 0/1/2; vuile werkmap = 1 | 2 is voorbehouden aan "verkeerd aangeroepen"; een vuile werkmap is een toestand, geen argumentfout. Manual is een uitkomst → 0 |
| Parsefouten → 2 via `ParseResult.Errors` na `InvokeAsync` | System.CommandLine geeft standaard 1; zo blijft de eigen help-/foutuitvoer behouden |
| `--fix` met `AcceptOnlyFromAmong(fix-ids)` | Onbekende fix faalt meteen als argumentfout (2), vóór het trage laden |
| Laadfouten = alleen `WorkspaceDiagnosticKind.Failure` → `SolutionLoadException`; warnings genegeerd | MSBuild geeft vaak onschuldige warnings; bij een failure klopt het SemanticModel niet en zijn rewrites onbetrouwbaar |
| `MsBuildSolutionStore` houdt één `MSBuildWorkspace` vast (IDisposable) | `TryApplyChanges` werkt alleen op de workspace die de solution laadde |
| Composition root in `Cli.cs`, `Program.cs` is één regel | Tests (`CliTests`, integratietest) draaien de echte CLI in-process, zonder `InternalsVisibleTo` of extra proces |
| `FixCommand` hergebruikt `ScanCommand.RunAsync` | Fix = scan + schrijven; één plek voor laden, rapport en foutvertaling |
| `IReportWriter.Extension` i.p.v. een aparte factory | Kiezen op extensie is één `FirstOrDefault`; een factory-klasse voegt niets toe |
| Rapportpaden relatief aan de solution-map, met `/` | Leesbaar en gelijk op Windows en Linux |
| Eigen LCS-regeldiff (`LineDiff`), common prefix/suffix eerst weg | Geen nieuw pakket nodig; rewrites zijn lokaal, dus de LCS-tabel blijft klein |
| Spectre-breedte 1000 bij omgeleide uitvoer | Anders breekt Spectre paden en diffregels op 80 kolommen (pipes, PowerShell-capture) |
| Fouten naar stderr (aparte `IAnsiConsole`) | Scripts kunnen stdout (tabel/diff) en fouten scheiden |
| CA1716 uit voor Interface | Projectnaam `Unshackle.Interface` ligt vast in de architectuur; alleen VB-keyword, exe zonder afnemers |
| Integratietest kopieert `samples/` zonder `bin/obj` naar een tempmap met spaties en draait `dotnet restore` | `obj/` bevat absolute paden; restore maakt de kopie echt laadbaar, ook in CI; spaties bewijzen REQ-10 |
| Integratietest checkt vaste rijen (`fa.should-be` regel 24, `fa.have-count` Manual) + "≥ 20 Fixed" i.p.v. exacte totalen | Parallelle patroon-agents voegen patronen toe; exacte totalen zouden bij elk nieuw patroon breken |
| Versie `0.1.0-preview` | Tool is installeerbaar maar nog niet af (projectbestanden niet gemigreerd) |
| Geen nieuwe NuGet-pakketten | Alles kon met wat er al stond (System.CommandLine, Spectre, CsvHelper, System.IO.Abstractions, MSBuild-workspaces) |

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `docs/projectinfo/architectuur/projecten.md` — afhankelijkheden Interface → Core ← DataAccess
- `docs/projectinfo/architectuur/rapportage.md` — rapportformaat
- `claude/tasks/archief/TAAK-001-fundament.md` — contract en keuzes
