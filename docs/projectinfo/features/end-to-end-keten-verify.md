# Verification: end-to-end-keten

|               |                                                                                  |
| ------------- | -------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/features/end-to-end-keten-plan.md`](end-to-end-keten-plan.md) |
| **Status**    | Implemented, met afwijkingen buiten scope (zie onder)                            |
| **Generated** | 2026-10-11 by KingmaDoc 0.3.0.dev9, handmatig beoordeeld (TAAK-005)              |

> Gestart met `kingmadoc verify` (commit 41ff4a3 + niet-gecommit werk). Andere agents werkten tegelijk aan
> FA-patronen; alleen de bestanden van deze feature zijn beoordeeld.

## Requirements

| REQ                        | Uitkomst         | Bewijs                                                                                                                                                                                                                                                                                                  |
| -------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 `list`               | gehaald          | `CliTests.RunAsync_List_ReturnsOk`, `CommandTests.ListExecute_Fixes_ShowsThemAndReturnsOk`; handmatig via geïnstalleerde tool (exit 0)                                                                                                                                                                  |
| REQ-2 `scan`               | gehaald          | `SampleShopTests.ScanDryRunFix_SampleShopCopy_ReportsThenRewrites` (bestand ongewijzigd na scan); Spectre-tabel Fixed/Manual per pattern                                                                                                                                                                |
| REQ-3 rapport              | gehaald          | `ReportWriterTests` (CSV/JSON/Markdown), `CommandTests.ScanExecuteAsync_ManualFindings_WritesRelativeReportAndReturnsOk` (relatieve paden), integratietest controleert CSV-rijen                                                                                                                        |
| REQ-4 `fix` schrijft       | gehaald          | `CommandTests.FixExecuteAsync_CleanTree_SavesAndReturnsOk`; integratietest: `order.Total.ShouldBe(25m);` en `using Shouldly;` op schijf                                                                                                                                                                 |
| REQ-5 vuile werkmap        | gehaald          | `CommandTests.FixExecuteAsync_DirtyTree_RefusesWithoutLoading`, `..._DirtyTreeWithForce_Saves`, `GitSourceControlTests` (3); handmatig in deze repo: exit 1                                                                                                                                             |
| REQ-6 `--dry-run`          | gehaald          | `CommandTests.FixExecuteAsync_DryRunOnDirtyTree_ShowsDiffAndWritesNothing`, `LineDiffTests` (3), integratietest (bestand ongewijzigd)                                                                                                                                                                   |
| REQ-7 laadfouten           | gehaald          | `CommandTests.ScanExecuteAsync_LoadFails_...`, `..._MissingSolution_...`, `CliTests.RunAsync_ScanMissingSolution_ReturnsFailure`, `MsBuildSolutionStoreTests`                                                                                                                                           |
| REQ-8 ongeldige argumenten | gehaald          | `CliTests.RunAsync_InvalidArguments_ReturnsInvalidArguments` (5 gevallen), `CommandTests.ScanExecuteAsync_UnknownReportExtension_...`                                                                                                                                                                   |
| REQ-9 Manual = 0           | gehaald          | `CommandTests.ScanExecuteAsync_ManualFindings_...`; integratietest (Manual-rij `fa.have-count`, exit 0)                                                                                                                                                                                                 |
| REQ-10 spaties in paden    | gehaald          | Integratietest draait in `.../unshackle tests/<guid>/my samples/...` met rapport in `report out/`; tool-test in `scratchpad/tool test/`                                                                                                                                                                 |
| REQ-11 `dotnet tool`       | gehaald          | Handmatig: `dotnet pack src/Unshackle.Interface -o "<scratch>/tool test"` → `Unshackle.0.1.0-preview.nupkg`; `dotnet tool install --tool-path ... --prerelease`; `unshackle list` (0), `scan ... --report r.csv` (0, 25 rijen), `fix` op vuile werkmap (1), `scan` zonder args (2), ontbrekende sln (1) |
| REQ-12 integratietest      | gehaald (lokaal) | `SampleShopTests`, trait `Category=Integration`, filterbaar met `-- --filter-trait "Category=Integration"`; restore via `dotnet restore` op de kopie, dus ook geschikt voor CI (ubuntu, git en dotnet aanwezig). CI zelf nog niet gedraaid                                                              |

## Afwijkingen van het plan

| #   | Plan                                           | Code                                                                                                | Impact                                                               |
| --- | ---------------------------------------------- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| 1   | `Program.cs` is composition root               | Composition root in `Cli.cs`; `Program.cs` roept alleen `Cli.RunAsync` aan                          | Laag: zo draaien tests de echte CLI in-process                       |
| 2   | —                                              | `.editorconfig`: CA1716 uit voor `src/Unshackle.Interface/**` en `src/Unshackle.Tests/Interface/**` | Laag: projectnaam ligt vast; "Interface" is alleen in VB een keyword |
| 3   | —                                              | `ScanResult`, `ExitCodes`, `CommandException`, `DiffLine`/`DiffKind`, `ReportRow` erbij             | Laag: kleine hulptypen, één per bestand                              |
| 4   | —                                              | Spectre-breedte 1000 als uitvoer omgeleid is                                                        | Laag: anders breekt Spectre paden en diffregels af op 80 kolommen    |
| 5   | KingmaDoc meldt honderden gewijzigde bestanden | Komen van de vorige commit-basis en van parallelle patroon-agents                                   | Geen: niet van deze feature                                          |

## Validation results

| Check      | Command                                                                              | Result                          |
| ---------- | ------------------------------------------------------------------------------------ | ------------------------------- |
| Build      | `dotnet build`                                                                       | 0 warnings, 0 errors            |
| Tests      | `dotnet test`                                                                        | 188 geslaagd, 0 mislukt         |
| Sample     | `dotnet test --solution samples/SampleShop/SampleShop.slnx`                          | 15 geslaagd                     |
| Integratie | `dotnet test --project src/Unshackle.Tests -- --filter-trait "Category=Integration"` | 1 geslaagd (~9 s incl. restore) |

## Open punten

- Projectbestanden migreren (FA-pakket eruit, Shouldly erin, `<Using Include>`): zonder dat bouwt de gefixte sample niet.
