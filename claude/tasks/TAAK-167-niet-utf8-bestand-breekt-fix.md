# TAAK-167 — Niet-UTF-8-bestand breekt de hele fix af

**Status**: stap 1/3 — nog te starten
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

- [ ] 1. DataAccess-test: een Latin-1-bestand in een solution geeft geen exception, wel een Manual.
- [ ] 2. Implementatie in `XmlProjectFileStore`/`FixCommand`, zonder algemene `catch (Exception)`.
- [ ] 3. Bewijs: het EventFlow-harnas zonder handmatige conversie geeft `ok`.

## Risico's & randgevallen

| Risico | Mitigatie |
| --- | --- |
| Het project verliest het FA-pakket terwijl één bestand nog FA gebruikt | De Manual telt mee voor de project-file-cascade |

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/docs/INDEX.md`
