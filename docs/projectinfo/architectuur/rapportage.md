# Rapportage

Elk formaat is een eigen `IReportWriter` in DataAccess (`CsvReportWriter`, `JsonReportWriter`,
`MarkdownReportWriter`). Interface kiest er een op basis van de extensie van `--report`.
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

## Zie ook

- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
- `docs/projectinfo/features/end-to-end-keten-plan.md` — plan van de keten (REQ-3)
