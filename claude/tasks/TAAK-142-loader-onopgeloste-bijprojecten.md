# TAAK-142 — Laden: projecten met onopgeloste types overslaan als ze geen gemigreerde bibliotheek gebruiken

**Status**: klaar (2026-10-11)
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
- [x] Corpus: `claude/tools/corpus-regression.sh` op akka en steeltoe

## Resultaat

**De oorspronkelijke 302/502 fouten waren grotendeels omgeving, niet de loader.** Bij het nalopen van `out/akka/work` bleek
`~/.nuget/packages` tussen restore en fix uitgedund (o.a. `akka.analyzers/0.3.3`, `newtonsoft.json/13.0.1` en de net8-targeting
packs weg): daardoor resolvede zelfs `Akka` (Newtonsoft) en Steeltoe (`Attribute`, `InternalsVisibleTo`) niet.

Verse run (`OUT=/mnt/dikkeschijf/unshackle-corpus/out142`, bronnen `src142/` = de oude werkkopieën zonder bin/obj, fix
`fluentassertions`, tool van deze branch):

| Solution | laden                                                                              | fix                                     | build na fix        |
| -------- | ---------------------------------------------------------------------------------- | --------------------------------------- | ------------------- |
| Akka.NET | ok (alleen waarschuwingen: SourceLink "Unable to locate repository", 2× `.fsproj`) | 8453 Fixed / 1899 Manual, 742 bestanden | faalt: 4962× CS0121 |
| Steeltoe | ok, geen waarschuwingen                                                            | 5043 Fixed / 755 Manual, 298 bestanden  | faalt: 58× CS1061   |

De nieuwe tolerantie sloeg in deze runs geen project over (geen "uses no migrated library"-waarschuwing): met een intacte
NuGet-cache resolven alle projecten. Ze blijft nuttig voor solutions met echt kapotte bijprojecten, maar is op het corpus niet
aangetoond.

Nieuwe productbevindingen (vervolgtaken, niet in deze taak):

- **Akka**: `Akka.TestKit.AkkaSpecExtensions.ShouldBe<T>` (eigen extensie, zelfde naam) maakt elke `ShouldBe` na de fix
  dubbelzinnig met Shouldly (CS0121).
- **Steeltoe**: `fa.contain-key` herschrijft `ContainKey` op `HttpResponseHeaders` en `IReadOnlyDictionary<,>` naar
  `ShouldContainKey`, die Shouldly alleen voor `IDictionary<,>` heeft (CS1061).

## Zie ook

- `claude/tasks/archief/TAAK-112-bogus-load-builder-project.md`
- `claude/tasks/archief/TAAK-135-loader-razor-en-vreemde-projecten.md`
- `claude/tasks/REGRESSIE-corpus.md`
