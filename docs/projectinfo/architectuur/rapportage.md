# Rapportage

Elk formaat is een eigen `IReportWriter` in DataAccess (`CsvReportWriter`, `JsonReportWriter`,
`MarkdownReportWriter`). Interface kiest er een op basis van de extensie van `--report`. Een bestaand bestand wordt alleen met `--force` overschreven.
Markdown-cellen worden HTML-ge-encodeerd en `\`, `|`, `` ` `` en regeleinden ge-escaped (redenen citeren gebruikerscode).

```csv
File,Line,Fix,Pattern,Status,Reason
tests/UserTests.cs,14,fluentassertions,fa.should-be,Fixed,
tests/OrderTests.cs,8,fluentassertions,fa.have-count,Manual,Dictionary: controleer of Count of Keys bedoeld is
```

Gebouwd in TAAK-005 (`src/Unshackle.DataAccess/Reports/`):

- `IReportWriter` (Core) heeft een `Extension` (`.csv`, `.json`, `.md`); `ScanCommand` kiest de writer met dezelfde
  extensie (hoofdletters maken niet uit). Onbekende extensie → exitcode 2, vóór het laden.
- `ReportRow.From(Finding)` maakt de kolommen; `Reason` is leeg bij `Fixed`.
- Paden in het rapport zijn relatief aan de map van de solution, met `/` als scheidingsteken (gelijk op elk OS).
- CSV via CsvHelper (quotes waar nodig), JSON via `System.Text.Json` (array van objecten met dezelfde namen),
  Markdown als tabel (`|` geëscaped). Alle writers schrijven via `IFileSystem`, dus testbaar met `MockFileSystem`.
- Kan het rapport niet geschreven worden (map bestaat niet, geen rechten) → exitcode 1.

## Eigen assertion-extensies gegroepeerd (TAAK-091)

- `fa.custom-assertion` geeft per aanroep van een eigen extensie op een FA-assertions-type een Manual met dezelfde reden
  per extensie ("Custom assertion `ResultAssertions.BeFailure` (ResultAssertions.cs): rewrite … once; its calls can stay").
- `ReportRow.FromAll` (alle drie de formaten) maakt daar één rij van per extensie: de eerste aanroep, reden
  voorafgegaan door "N call(s), first here.". Andere findings blijven één rij per finding. Gevolg: wie Manual-rijen
  telt, telt deze extensies één keer.
- De console (`SpectreConsoleView.ShowSummary`) toont onder de tabel één advieskop en per extensie `Nx reden`, aflopend.
- Groeperen gebeurt op de exacte reden via `CustomAssertionPattern.Group`; de "Not rewritten: …"-Manuals van
  AllOrNothing-fixes (MediatR) hebben een ander patroon-id en blijven per finding.

## Zie ook

- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
- `docs/projectinfo/features/end-to-end-keten-plan.md` — plan van de keten (REQ-3)
