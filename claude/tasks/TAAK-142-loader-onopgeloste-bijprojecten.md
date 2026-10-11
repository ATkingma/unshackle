# TAAK-142 — Laden: projecten met onopgeloste types overslaan als ze geen gemigreerde bibliotheek gebruiken

**Status**: in uitvoering
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/REGRESSIE-corpus.md` (Akka.NET 302, Steeltoe 502 fouten)

## Probleem

`MsBuildSolutionStore.LoadAsync` weigert de hele solution zodra één project CS0246/CS0234 geeft. Bij Akka.NET en Steeltoe
waren dat AOT-apps, benchmarks en samples (`Akka.Hosting.AOT.App`, `Akka.Benchmarks`, `RemotePingPong`,
`Steeltoe.Management.GitProperties.Build`, `ConfigurationSchemaGenerator`): ze gebruiken geen FluentAssertions of MediatR,
maar hielden de hele migratie tegen. TAAK-112 liet alleen projecten zonder `System` vallen, TAAK-135 alleen projecten die
helemaal niet laden.

## Wat, waar, hoe

- **Waar**: `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs` (de CS0246-check).
- **Hoe**: een project met onopgeloste namen wordt met waarschuwing uit de solution gelaten als (1) geen project ervan afhangt,
  (2) het geen gemigreerde bibliotheek gebruikt (geen assembly-referentie en geen onopgeloste naam die met een namespace van een
  fix begint) en (3) minstens één ander project wel resolvet. Anders blijft het fataal (een project dat FA gebruikt maar niet
  resolvet zou gemist worden; een volledig ongerestorede solution geeft nog steeds het restore-advies).
- Welke bibliotheken: `IFix.RemovedNamespaces` van alle fixes, doorgegeven in de composition root (`Cli.cs`) aan de constructor.
  `ISolutionStore` blijft ongewijzigd.

## Stappen

- [x] Rode test: `SampleShopTests.Load_UnresolvedLeafProject_IsSkippedUnlessItUsesAMigratedLibrary` (2 gevallen)
- [x] Tolerantie + constructorparameter
- [x] `dotnet build`, `dotnet test` (695), sample-tests (38) groen
- [ ] Corpus: `claude/tools/corpus-regression.sh` op akka en steeltoe

## Resultaat

(volgt na corpus-run)

## Zie ook

- `claude/tasks/archief/TAAK-112-bogus-load-builder-project.md`
- `claude/tasks/archief/TAAK-135-loader-razor-en-vreemde-projecten.md`
- `claude/tasks/REGRESSIE-corpus.md`
