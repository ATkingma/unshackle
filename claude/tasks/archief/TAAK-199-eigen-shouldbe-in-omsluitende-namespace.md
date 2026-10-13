# TAAK-199 — Eigen `ShouldBe` in de omsluitende namespace verdringt Shouldly stil (geen CS0121)

**Status**: ✅ voltooid (2026-10-13)
**Aangemaakt**: 2026-10-13 (TAAK-174 stap 3, Akka-herhaling)
**Doel**: een herschreven call die niet aan Shouldly bindt, maar aan een eigen extensie, wordt Manual in plaats van stil van gedrag te veranderen.

## Context voor de volgende agent

- Akka.NET (`23a8566`), testregressie na de fix: `Akka.Tests.Serialization.V2PortKit.MixedBindingPairSpec.Should_FailTheCheck_When_TheV2NodeIsStillPinnedToLegacy`.
- Oorzaak: `src/core/Akka.Tests.Shared.Internals.Xunit3/V2Port/MixedBindingPair.cs` staat in `namespace Akka.TestKit;`. `serializer.Identifier.Should().Be(writerId, "{0}: …", stage, c.Name)` werd `serializer.Identifier.ShouldBe(writerId, string.Format(…))`. Die call bindt aan `Akka.TestKit.AkkaSpecExtensions.ShouldBe<T>(T, T, string)` (extensies uit de omsluitende namespace gaan vóór `using`-imports), niet aan Shouldly. Akka's versie gooit een xUnit `EqualException` zonder de meegegeven tekst, dus de test die op "V2 node to legacy node" in de foutmelding controleert, faalt.
- TAAK-174 zoekt alleen naar CS0121 (dubbelzinnigheid). Hier is er geen dubbelzinnigheid, dus de check ziet niets.

## Analyse

Na de rewrite per herschreven invocation semantisch controleren dat het gebonden symbool in de Shouldly-assembly (of `Should`) zit. Bindt de call aan iets anders: bestand Manual met reden "binds to <type>.<methode> instead of Shouldly". Zelfde plek als `UsingDirectives.AmbiguityWithAddedAsync` (`Fixer.cs`). Ook relevant voor TAAK-198 (aanpak 2).

## Stappen

- [x] 1. Unittest: bestand in een namespace met een eigen `ShouldBe<T>(this T, T, string)`; FA-call wordt nu stil herschreven.
- [x] 2. Bindingscheck na de rewrite (alle herschreven Shouldly-calls), Manual als het symbool niet uit Shouldly komt.
- [x] 3. Proof in `src/Unshackle.Tests/Proofs/FluentAssertions/…` (`*.manual.txt`).
- [x] 4. Bewijs: Akka-herhaling, `MixedBindingPairSpec` groen.

## Resultaat

- `UsingDirectives.MisboundAddedCallAsync`: zoekt in het bijgewerkte bestand aanroepen met de naam van een extensie uit een toegevoegde namespace (`ShouldBe`, `ShouldBeTrue` …) die aan iets buiten die namespaces binden, en vergelijkt het aantal per naam met het origineel (bestaande eigen `ShouldBe`-aanroepen tellen dus niet). Meer dan in het origineel → `Fixer` valt terug zoals bij CS0121 (TAAK-174): bestand ongewijzigd, Fixed → Manual "Not rewritten: …", plus Manual `usings` "a rewritten ShouldBe() binds to <type>.ShouldBe instead of Shouldly …".
- Unittests: `AmbiguousAddedNamespaceTests.FixAsync_FileInNamespaceOfOwnShouldBe_LeavesFileAndReportsManual` en `…AlreadyUsingIt_MigratesOtherCalls`.
- Proof: `Proofs/FluentAssertions/ShouldBe/own-shouldbe-in-namespace.*` (`.manual.txt`). `ProofTests` geeft de FA-fix nu Shouldly als `AddedReferences` mee (zoals de echte fix), anders ziet het model Shouldly niet; alle 773 proofs blijven groen.
- Akka (verse fix op `23a8566`): 10 bestanden (54 findings) vallen op deze regel terug, o.a. `MixedBindingPair.cs`, `V2PortSpec.cs` en 7 bestanden in `Akka.TestKit.Tests`. Akka.Tests: 1553 geslaagd, 0 gefaald (incl. `MixedBindingPairSpec`); build 0 fouten.
## Zie ook

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/TAAK-198-shouldly-conflict-manual-verkleinen.md`
- `claude/tasks/CORPUSRUN-fa.md`
