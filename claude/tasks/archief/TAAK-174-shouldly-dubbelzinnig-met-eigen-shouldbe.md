# TAAK-174 — `using Shouldly;` maakt eigen `ShouldBe`-extensies dubbelzinnig (CS0121)

**Status**: voltooid (2026-10-13)
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

- [x] 1. Unittest met een eigen statische `ShouldBe<T>`-extensie in scope.
- [x] 2. Conflictdetectie in de using-stap (`UsingDirectives.cs`) of als pre-check in de Fixer.
- [x] 3. Bewijs: Akka-harnas bouwt; de overige foutcodes uitzoeken en zo nodig een vervolgtaak maken.

## Resultaat stap 3 (2026-10-13)

Akka.NET `23a8566`, verse kloon in `<probe-dir>`, `unshackle fix Akka.slnx --fix fluentassertions --force`, daarna bin/obj weg, restore + build met `-p:NuGetAudit=false -p:AllowMissingPrunePackageData=true`:

- Fix: **5 463 Fixed / 5 126 Manual** (zonder `project-file`/`usings`; 441 bestanden geschreven). Daarvan 3 449 Manual met "… is ambiguous with Akka.TestKit.AkkaSpecExtensions.… once Shouldly is imported (CS0121)".
- **Build: 0 fouten** (was 4 984× CS0121 + 8× CS8130, 8× CS0119, 6× CS1061, 6× CS0452). De overige codes zijn mee verdwenen; de oude log bevat geen regelinformatie meer, ze zaten in bestanden die nu Manual zijn (gevolgfouten van de dubbelzinnigheid).
- Tests (hele solution, 46 assemblies) tegenover de twee baseline-runs: bekende falers (Cluster `ProviderSelectionSpec`, DData, Streams `TcpSpec`) gelijk. Nieuw:
  - `MixedBindingPairSpec.Should_FailTheCheck_When_TheV2NodeIsStillPinnedToLegacy` — echte regressie: eigen `ShouldBe` in de omsluitende namespace `Akka.TestKit` wint stil van Shouldly → TAAK-199.
  - `ReplicatorMessageSerializerSpec.ReplicatorMessageSerializer_should_serialize_Replicator_message` — echte regressie (los 2× rood): `object`-subject `ShouldBe` → TAAK-200.
  - `ConductorBindSpec` (3×, poort 2552 bezet) en `ClusterShardingGetStateSpec` (MultiNode, `ExpectMsg`-timeout; diff is equivalent) — flaky: Remote.TestKit los 40/40 groen.
- Vervolg: TAAK-198 (conflict-Manual verkleinen), TAAK-199, TAAK-200.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/tasks/TAAK-198-shouldly-conflict-manual-verkleinen.md`
- `claude/tasks/TAAK-199-eigen-shouldbe-in-omsluitende-namespace.md`
- `claude/tasks/TAAK-200-shouldbe-object-gelijkheid-ddata.md`
- `claude/tasks/archief/TAAK-165-custom-assertions-should-extensie.md`
