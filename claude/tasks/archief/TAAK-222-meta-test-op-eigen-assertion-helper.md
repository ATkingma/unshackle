# TAAK-222 — `Throw<XunitException>()` op een eigen assertion-helper in een ander bestand → helper gemigreerd, verwachte exception klopt niet meer

**Status**: ✅ voltooid (2026-10-13)
**Aangemaakt**: 2026-10-13 (FA-corpusrun ronde 4)
**Doel**: geen regressie als een test controleert dat een eigen assertion-helper faalt met de exception van FA/xUnit.

## Context voor de volgende agent

- LiteDB `7b71bc4`: 26 regressies (13 tests × net8.0/net10.0) in `LiteDB.Tests/Query/Int32IdAssertions_Tests.cs`.
- De eigen helper `Int32IdAssertions.BeEquivalentTo(..)` in `Int32IdAssertions.cs` gebruikt FA en is gemigreerd naar Shouldly. De tests doen `Action candidate = () => Int32IdAssertions.BeEquivalentTo(..); candidate.Should().Throw<XunitException>()` en krijgen nu een `ShouldAssertException`.
- TAAK-070 vangt dit alleen als de assertion in de lambda zelf staat. Hier zit ze in een methode in een ander bestand.

## Stappen

- [x] 1. In `fa.throw`: als `T` een assertion-exceptiontype is (`XunitException`, FA `AssertionFailedException`, NUnit `AssertionException`, MSTest `AssertFailedException`) en de delegate (ook via een lokale) een methode uit de eigen solution aanroept die FA-asserts bevat → Manual, met als reden "the helper is migrated to Shouldly and throws ShouldAssertException".
- [x] 2. Alternatief overwegen: het helper-bestand Manual laten, zodat beide kanten FA blijven.
- [x] 3. Unittests + proof; LiteDB-herhaling met 0 regressies.

## Resultaat

Corpus ronde 5 (`CORPUSRUN-fa.md`): Akka 1 regressie (alleen TAAK-200), Steeltoe 0, LiteDB 0. Build, `dotnet test` (1093) en sample-tests (38) groen.

## Keuzes

- Alleen de test Manual maken helpt niet: de helper wordt dan nog steeds gemigreerd en gooit `ShouldAssertException`. `Throw<XunitException>` omzetten naar `ShouldAssertException` is fragiel (blijft één helper-assert Manual, dan gooit de helper weer `XunitException`) en `WithMessage` met een `*`-midden-patroon wordt toch Manual. **Gekozen: alternatief 2**, helper én test blijven FA.
- `ExpectsAssertionFailure` volgt bij een assertion-exception als `T` (`XunitException`, `AssertionFailedException`, `AssertionException`, `AssertFailedException`, of afgeleid) de methoden met broncode die de actie aanroept (twee niveaus): bevat er een een FA-assertion → de `Throw` wordt Manual.
- `IsInsideExpectedFailure` (alle FA-patronen) is ook waar binnen zo'n helper: per syntax tree (gecachet in een `ConditionalWeakTable<SyntaxTree, …>`; bestanden met een van die namen in de tekst) worden de helpers verzameld die een `Throw<assertion-exception>`-actie aanroept. Sleutel: type + naam + aantal parameters, stabiel over de compilaties die de Fixer per bestand maakt.
- Bij andere `T` (bv. `InvalidOperationException`) blijft de helper gewoon gemigreerd (unittest).
- Proofs: `Throw/assertion-helper`, `BeTrueFalse/assertion-helper`; unittests in `FluentAssertionsSyntaxTests` met de helper in een ander bestand.

## Zie ook

- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md`
- `claude/tasks/CORPUSRUN-fa.md` (ronde 4)
- `claude/tasks/patroon-queue.md`
