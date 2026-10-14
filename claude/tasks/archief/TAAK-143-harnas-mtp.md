# TAAK-143 — Regressieharnas ondersteunt Microsoft.Testing.Platform

**Status**: voltooid
**Aangemaakt**: 2026-10-11 (regressie-run, `claude/tasks/REGRESSIE-corpus.md`)

## Context

Het harnas haalde `test.runner` uit elke `global.json` om VSTest (`--logger trx`) af te dwingen. PowerApps-Tooling gebruikt MSTest.Sdk (MTP); de .NET 10 SDK weigert MTP-projecten onder VSTest → 0 tests, regressie-oordeel onmogelijk.

## Wijziging

- `corpus-regression.py globaljson`: detecteert MTP via `global.json` `test.runner` of projecteigenschappen (`Sdk="MSTest.Sdk`, `UseMicrosoftTestingPlatformRunner`, `EnableMSTestRunner`, `EnableNUnitRunner`, `TestingPlatformDotnetTestSupport`). Bij MTP blijft/komt `test.runner` erin; anders ongewijzigd (VSTest). Print `mtp` of `vstest`. Selftest uitgebreid.
- `corpus-regression.sh`: bij `mtp` → `dotnet test --solution <sln> --no-build --report-trx --results-directory <dir>`. Zelfde TRX-vergelijking per test.
- Beperking: in MTP-modus alleen een solution als doel (geen `.csproj`); gemengde solutions (VSTest + MTP) niet getest.

## Bewijs (2026-10-11, tool uit `out/tool`, verse GitHub-tarballs in `<corpus>/src/`, uitvoer `<corpus>/t143/`)

| Solution                                             | runner | vóór          | na            | regressies |
| ---------------------------------------------------- | ------ | ------------- | ------------- | ---------- |
| powerapps (fluentassertions, 293 Fixed / 103 Manual) | MTP    | 1178 (1172/6) | 1178 (1172/6) | 0          |
| ardalis (mediatr; huidige upstream, fix vond niets)  | VSTest | 20 (19/1)     | 20 (19/1)     | 0          |

## Zie ook

- `claude/tasks/REGRESSIE-corpus.md`
- `claude/tools/corpus-regression.sh`
