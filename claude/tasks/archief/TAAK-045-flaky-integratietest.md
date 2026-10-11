# TAAK-045 — Flaky integratietest `FixTwice_SampleShopCopy_MigratesProjectFilesAndBuildsAndTests`

**Status**: ✅ voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Doel**: de integratietest in `src/Unshackle.Tests/Integration/SampleShopTests.cs` slaagt ook bij de eerste `dotnet test` in een verse (koude) worktree, zonder de asserts af te zwakken.

## Melding

Faalde soms bij de eerste `dotnet test` in een verse worktree (geen `obj/`/`bin/`), slaagde bij herhaling en los. Hint: een scan vóór `dotnet restore` geeft 0 findings.

## Reproductie

- Koude kopie (rsync zonder `bin/obj`) + `dotnet build` + `dotnet test`: 7 runs sequentieel en 6 runs (3 parallel) — **niet gereproduceerd**, alles groen. De oorspronkelijke foutmelding is dus niet vastgelegd.
- Wel gevonden (`/proc/<pid>/environ` van de testhost): de buitenste `dotnet test` zet `MSBuildLoadMicrosoftTargetsReadOnly`, `MSBUILDFAILONDRIVEENUMERATINGWILDCARD`, `_MSBUILDTLENABLED` en `TESTINGPLATFORM_*` in de testhost. De `dotnet restore/build/test`-kindprocessen van de test erfden die, en gebruikten MSBuild node reuse en de MSBuild-server die de koude build van de repo net had achtergelaten.

## Oorzaak (meest waarschijnlijk)

De kind-`dotnet`-processen waren niet geïsoleerd van de buitenste build/test: hergebruikte MSBuild-nodes/server uit de koude build en gelekte MSBuild-/testplatform-omgevingsvariabelen. Bij een koude run (nodes net gestart, druk systeem, eventueel andere agents die tegelijk bouwen) kan de restore van de kopie dan anders/onvolledig uitpakken, waarna MSBuildWorkspace zonder geldige assets laadt → 0 findings → assert op de rapportregels faalt (past bij de hint). Racen tussen de integratietests zelf is uitgesloten: ze zitten in één class (= één xUnit-collection, sequentieel) en elk gebruikt een eigen `TempDirectory` (GUID).

## Fix

`DotnetAsync` in `SampleShopTests.cs`: verwijdert overgeërfde `MSBuild*`, `_MSBuild*` en `TESTINGPLATFORM_*` variabelen en zet `MSBUILDDISABLENODEREUSE=1` en `DOTNET_CLI_USE_MSBUILD_SERVER=false`. Asserts ongewijzigd; restore stond al expliciet in elke test.

## Bewijs

- 5 opeenvolgende koude runs (verse kopie zonder `bin/obj`, `dotnet build` + `dotnet test`): 5× 346/346 groen.
- `dotnet build` 0 fouten, `dotnet test` 346/346, `dotnet test --solution samples/SampleShop/SampleShop.slnx` 38/38, hook `check-pattern-docs.sh` exit 0.

## Restrisico

Omdat de fout niet lokaal reproduceerbaar was, is de oorzaak niet hard bewezen. Faalt hij opnieuw: leg de volledige Shouldly-melding vast (bevat stdout/stderr van `dotnet` of de rapportregels).

## Zie ook

- `claude/docs/agent-werkwijze.md` §6 — main blijft groen
- `src/Unshackle.Tests/Integration/SampleShopTests.cs`
