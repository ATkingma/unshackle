# TAAK-171 — `ContainKey(k).WhoseValue` en `ContainKeys(..).And.ContainValues(..)`

**Status**: ✅ voltooid — 2026-10-12 (stap 3: harnas niet herdraaid, zie Keuzes)
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: de 377 Manuals rond `ContainKey`/`ContainKeys`/`ContainValues` (Steeltoe 249, Polly 120) worden Fixed waar dat veilig kan.

## Context voor de volgende agent

- In Steeltoe is de reden "The result of ContainKey(...) is used (.And/.Which or a value)"; meestal gaat het om `.ContainKey(k).WhoseValue.Should().Be(v)`.
- Polly 7.2.4, `src/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerAsyncSpecs.cs:2576`: `contextData.Should().ContainKeys("key1", "key2").And.ContainValues("value1", "value2");`
- Nu maakt `ContainKeyPattern.cs:19-31` alle Keys/Values-varianten meteen Manual.

## Nieuwe situatie

- `ContainKey(k).WhoseValue.Should().Be(v)` → `x.ShouldContainKeyAndValue(k, v)`.
- `ContainKey(k)` met een andere vervolg-assertie → `x.ShouldContainKey(k);` gevolgd door `x[k].<rest>`, maar alleen als `x` een lokale variabele, parameter of veld is.
- `ContainKeys(a, b)` → één `ShouldContainKey` per key, als losse statements.
- `ContainValues(a, b)` → `x.Values.ShouldContain(a)` per value.
- De typebeperking uit TAAK-168 geldt hier ook.

## Stappen

- [x] 1. Proofs voor de drie vormen (`Proofs/FluentAssertions/AndChain/`: `contain-keys*`, `contain-value*`, `not-contain-*`, `whose-value*`).
- [x] 2. Implementatie in `AndChainPattern` (`ContainKeyPattern` bleef ongewijzigd).
- [x] 3. Bewijs: de Polly-regel staat letterlijk als proof `contain-keys-values`, het Steeltoe-geval als `whose-value`. De corpus-kloons zijn weg; het harnas is niet herdraaid (hoort bij de volgende corpus-ronde).

## Keuzes

- De splitsing zit al in `AndChainPattern`; de nieuwe vormen zijn "uitbreidingen van een keten": een `entries`-lijst met één FA-statement per uitkomst, daarna dezelfde `RewriteLink`-lus. Daardoor beslist `ContainKeyPattern`/`ContainPattern` per statement (ook `IReadOnlyDictionary`, `because`, nullable), zonder logica te dupliceren.
- `ContainKey(k).WhoseValue.Should().Be(v)` wordt `x.ShouldContainKey(k); x[k].ShouldBe(v);`, niet `ShouldContainKeyAndValue`: zo gelden de bewezen regels voor het waarde-patroon (typeconversie, because, strings) en werkt elk vervolg (`StartWith`, `.And`). Vereisten: x heeft een publieke indexer op het key-type; k is constant of veilig twee keer te evalueren; anders Manual. x telt als subject voor de hoist (`subject2` omdat `Y.WhoseValue` ook meetelt).
- `ContainKeys`/`ContainValues`: alleen de params-vorm met losse argumenten (via `IInvocationOperation`, `ArgumentKind.ParamArray`); collectie of becauseArgs is Manual. Ook `NotContainKeys`, `NotContainValues` en de enkelvoudige `ContainValue`/`NotContainValue` (die houden `because`) zijn meegenomen, want ze hebben dezelfde uitkomst: `x.Values.Should().Contain(v)` voor `ContainPattern`.
- Probe FA 7.2.2 vs Shouldly 4.3.0: `ContainValue` en `Values.ShouldContain` falen allebei op een array met gelijke inhoud en op `"X"` tegenover `"x"`; geen verzwakking.
- Nullable dictionary bij `ContainValue(s)`: `x!.Values` (een null-subject faalt met een NullReferenceException; de test blijft rood).
- `.WhoseValue` zonder `.Should()` erachter (`var v = x.Should().ContainKey(k).WhoseValue;`) blijft Manual (bestaand proof `ContainKey/which`).

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/` (27 nieuwe proofs)
- `docs/projectinfo/patterns/fa-and-chain-{plan,modellen,verify}.md` (REQ-10..13), `docs/projectinfo/README.md`, `docs/projectinfo/architectuur/proofs.md`, `claude/docs/INDEX.md`, `claude/tasks/patroon-queue.md`

## Zie ook

- `claude/tasks/TAAK-168-should-contain-key-readonly-dictionary.md`
- `claude/tasks/CORPUSRUN-fa.md`
