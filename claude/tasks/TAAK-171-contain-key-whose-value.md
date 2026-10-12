# TAAK-171 — `ContainKey(k).WhoseValue` en `ContainKeys(..).And.ContainValues(..)`

**Status**: stap 1/3 — nog te starten
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

- [ ] 1. Proofs voor de drie vormen.
- [ ] 2. Implementatie in `ContainKeyPattern` en `AndChainPattern`.
- [ ] 3. Bewijs: Steeltoe- en Polly-harnas.

## Zie ook

- `claude/tasks/TAAK-168-should-contain-key-readonly-dictionary.md`
- `claude/tasks/CORPUSRUN-fa.md`
