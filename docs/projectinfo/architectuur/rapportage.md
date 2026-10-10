# Rapportage

Elk formaat is een eigen `IReportWriter` in DataAccess (`CsvReportWriter`, `JsonReportWriter`,
`MarkdownReportWriter`). Interface kiest er een op basis van de extensie van `--report`.
```csv
File,Line,Fix,Pattern,Status,Reason
tests/UserTests.cs,14,fluentassertions,fa.should-be,Fixed,
tests/OrderTests.cs,8,fluentassertions,fa.have-count,Manual,Dictionary: controleer of Count of Keys bedoeld is
```

Nog niet gebouwd.

## Zie ook

- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
