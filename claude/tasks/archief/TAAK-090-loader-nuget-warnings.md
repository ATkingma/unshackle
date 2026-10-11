# TAAK-090 — loader: NuGet-warnings en ontbrekende projecten niet fataal

**Status**: voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md`, actie 0

## Probleem

5 van 10 corpus-solutions laadden niet. Polly, EventFlow, SmartEnum: NuGet-audit (NU1902/NU1903). Bogus: NU1701.
Specification: project ontbreekt in de tarball. Voor gebruikers erger dan elke Manual.

## Analyse

- Reproductie (scratchpad, alleen restore + laden): Polly faalde met "Msbuild failed when processing the file
  '…Polly.Benchmarks.csproj' with message: Package … has a known high severity vulnerability". Polly heeft géén
  TreatWarningsAsErrors: Roslyn's `MSBuildWorkspace` meldt elke MSBuild-melding als `WorkspaceDiagnosticKind.Failure`,
  ook een warning. Onze handler maakte van elke Failure een `SolutionLoadException`.
- `NuGetAudit=false` alleen helpt niet: de warnings staan al in `project.assets.json` en worden bij het design-time
  build opnieuw gelogd. Met TreatWarningsAsErrors worden het errors die `ResolvePackageAssets` laten falen, vandaar ook
  `WarningsNotAsErrors`.

## Wat, waar, hoe

- **Waar**: `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`, `ISolutionStore.Warnings` (Core),
  `ScanCommand` (toont warnings).
- **Hoe**: globale properties `NuGetAudit=false`, `WarningsNotAsErrors=NU1701;NU1900..NU1905` bij `MSBuildWorkspace.Create`.
  Failures worden na het laden ingedeeld: noemt de melding een projectbestand dat geladen is, of een dat niet bestaat →
  waarschuwing; anders fataal. De CS0246/CS0234-check blijft ongewijzigd en is het vangnet (hij krijgt de
  waarschuwingen mee in de foutmelding).
- **Waarom ontbrekend project veilig**: niets op schijf om te herschrijven; afhankelijke projecten falen alsnog op de
  CS0246-check. Een bestaand maar niet-geladen project blijft fataal (fix zou zijn code overslaan maar gedeelde
  pakketbestanden wel wijzigen).
- **Alternatief afgewezen**: herkennen op NU-code in de tekst (de melding bevat geen code en is gelokaliseerd).

## Resultaat

| Solution      | Voor                      | Na                                                                              |
| ------------- | ------------------------- | ------------------------------------------------------------------------------- |
| Polly         | faalt (NU1903)            | laadt, 4285 Fixed / 804 Manual                                                  |
| EventFlow     | faalt (NU1902/3 as error) | laadt, 459 / 65 (restore zelf vereist `-p:NuGetAudit=false`)                    |
| SmartEnum     | faalt                     | laadt, 299 / 68                                                                 |
| Bogus         | faalt (NU1701)            | laadt, 796 / 154 (na restore van `Builder`, dat de solution-restore oversloeg)  |
| Specification | faalt                     | faalt terecht: kernproject buiten de tarball → CS0246 in afhankelijke projecten |

Tests: `SampleShopTests.Load_VulnerablePackageAndMissingProject_LoadsRestWithWarnings` (integratie),
`CommandTests.ScanExecuteAsync_LoadWarnings_ShowsThemAndReturnsOk`. `Scan_NotRestored_FailsWithRestoreAdvice` blijft groen.

## Zie ook

- `docs/projectinfo/architectuur/core.md` — sectie TAAK-090
- `claude/tasks/ANALYSE-fa-100.md`
