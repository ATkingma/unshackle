# TAAK-167 — Niet-UTF-8-bestand breekt de hele fix af

**Status**: ✅ voltooid — 2026-10-12
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: één Latin-1-bestand levert een Manual op voor dat bestand, en de rest van de solution wordt wel gemigreerd.

## Context voor de volgende agent

- EventFlow `v0.83.4713` (`e544b76`): `Source/EventFlow.EntityFramework.Tests/MsSql/EfMsSqlReadStoreIncludeTests.cs` is ISO-8859-1.
- `fix` meldde 483 Fixed / 59 Manual en eindigde daarna met `Unhandled exception: InvalidDataException: … is not UTF-8` (exit 1). Er werd niets geschreven.
- Na het bestand met de hand naar UTF-8 om te zetten: fix en build groen, 970 tests, 0 regressies.

## Analyse

### Huidige situatie

`XmlProjectFileStore.Decode` (`src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs:396-405`) gooit `InvalidDataException`. `SaveAsync` (:143) controleert alle bestanden vóór het schrijven, en `FixCommand.ExecuteAsync` (:93) vangt de exception niet af.

### Probleem

Een fout in één bestand blokkeert de hele migratie, en de gebruiker krijgt een stacktrace in plaats van een regel in het rapport.

### Nieuwe situatie

Het bestand wordt overgeslagen met een Manual ("not UTF-8; convert it or migrate it by hand"). Het FA-pakket blijft in dat project staan (project-file-cascade); de rest van de solution wordt geschreven.

### Verbetering

De run gaat niet meer stuk, en de gebruiker ziet één regel in het rapport.

## Aanpak

- De encoding vóór de rewrite controleren (bij het laden in DataAccess) en het resultaat als "bestand niet bewerkbaar" teruggeven aan Core → Manual-finding.
- Alternatief: terugvallen op Latin-1 en die codering bij het schrijven behouden. Afgewezen: de echte codepagina is niet bekend.

## Stappen

- [x] 1. DataAccess-test: een Latin-1-bestand in een solution geeft geen exception, wel een Manual.
- [x] 2. Implementatie in `XmlProjectFileStore`/`FixCommand`, zonder algemene `catch (Exception)`.
- [x] 3. Bewijs: het EventFlow-harnas zonder handmatige conversie geeft `ok`.

## Keuzes en uitkomst

- De crash zat niet in `Decode` voor projectbestanden maar in `SaveAsync` voor het gewijzigde `.cs`-document; `FixCommand` ving `InvalidDataException` niet af.
- Detectie in Core (`Fixer.NotUtf8FilesAsync`), zonder I/O: Roslyn leest UTF-8 en BOM-bestanden met `UTF8Encoding` en alles anders met
  de eigen codering. Zo blijft Core vrij van DataAccess en is er geen contractwijziging.
- Het bestand wordt vóór alle patterns, global-using-swap en using-update overgeslagen, dus nooit gewijzigd en nooit naar `SaveAsync` gestuurd.
  Het noemt nog de oude bibliotheek, dus de bestaande cascade houdt het pakket in dat project.
- Alleen een bestand dat de oude bibliotheek noemt krijgt een Manual (`encoding`); anders ruis.
- Latin-1 als terugval met behoud van de codering: afgewezen (codepagina onbekend).
- `FixCommand` vangt nu ook `InvalidDataException` uit `SaveAsync` (race: bestand veranderd na laden) als nette foutmelding, geen algemene `catch`.
- Geen proofs-map: een proof is een `.cs` in UTF-8; de codering zit in de tests (Fixer-unit met `SourceText.From(code, Encoding.Latin1)` en een
  integratietest op schijf met een echt Latin-1-bestand).

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Interface/Commands/FixCommand.cs`
- `src/Unshackle.Tests/Core/FixerProjectFileTests.cs`, `src/Unshackle.Tests/Integration/SampleShopTests.cs`
- `docs/projectinfo/architectuur/rapportage.md`

## Risico's & randgevallen

| Risico | Mitigatie |
| --- | --- |
| Het project verliest het FA-pakket terwijl één bestand nog FA gebruikt | De Manual telt mee voor de project-file-cascade |

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/docs/INDEX.md`
