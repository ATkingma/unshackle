# TAAK-174 — `using Shouldly;` maakt eigen `ShouldBe`-extensies dubbelzinnig (CS0121)

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: Akka.NET bouwt na de fix, of krijgt een duidelijke Manual in plaats van een kapotte build.

## Context voor de volgende agent

- Akka.NET (`23a8566`): na de fix **4 984× CS0121** "ambiguous between `Akka.TestKit.AkkaSpecExtensions.ShouldBe<T>(T, T, string)` and `Shouldly.ShouldBeTestExtensions.ShouldBe<T>`". Voorbeeld: `src/contrib/cluster/Akka.Cluster.Sharding.Tests.MultiNode/ClusterShardingCustomShardAllocationSpec.cs:221`. Daarnaast enkele CS8130, CS0119, CS1061 en CS0452 (nog niet uitgezocht).
- De fix rapporteerde 8 488 Fixed / 1 868 Manual. Baseline: de grote assemblies zijn groen, met 2–3 bekende falers in Streams/Cluster.
- Verwant: TAAK-165 (eigen `Should()`-extensie) en "TAAK-150" (eigen `ShouldBe`) uit `REGRESSIE-corpus-2.md`. Dat laatste taakbestand is nooit aangemaakt.

## Analyse

### Probleem

Bestanden die al `Akka.TestKit` importeren (met o.a. `ShouldBe` en `ShouldBeTrue`) krijgen `using Shouldly;` erbij. Daardoor worden zowel de bestaande als de nieuw geschreven calls dubbelzinnig.

### Nieuwe situatie

Vóór `using Shouldly;` wordt toegevoegd, controleert de Fixer semantisch of er in scope al extensiemethoden met Shouldly-namen staan (`ShouldBe`, `ShouldBeTrue`, `ShouldContain`, …). Zo ja, dan wordt het bestand Manual ("conflicting Should* extensions from X") en blijft het FA-pakket in het project (project-file-cascade). Volledig gekwalificeerde Shouldly-aanroepen zijn afgewezen omdat ze onleesbaar worden.

## Stappen

- [ ] 1. Unittest met een eigen statische `ShouldBe<T>`-extensie in scope.
- [ ] 2. Conflictdetectie in de using-stap (`UsingDirectives.cs`) of als pre-check in de Fixer.
- [ ] 3. Bewijs: Akka-harnas bouwt; de overige foutcodes uitzoeken en zo nodig een vervolgtaak maken.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/tasks/archief/TAAK-165-custom-assertions-should-extensie.md`
