# TAAK-161 — Patroon fa.not-contain-nulls

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: `NotContainNulls()` → `ShouldNotContain(i => i == null)`; `AllBe("v")` → `ShouldAllBe(i => i == "v")`.

## Context voor de volgende agent

- Rij `fa.not-contain-nulls` in `claude/tasks/patroon-queue.md`.

## Analyse

FA 7.2.2: `AllBe` bestaat alleen op `StringCollectionAssertions` (een proof met `List<int>` compileerde niet); `NotContainNulls()` en `NotContainNulls(selector)` op `GenericCollectionAssertions`. Shouldly 4.3.0: `ShouldNotContain(Expression<Func<T,bool>>)` en `ShouldAllBe(Expression<Func<T,bool>>)`, beide met `customMessage`. Voorkomens in samples: `AdvancedAssertionTests.cs` (`.And.NotContainNulls()` via and-chain, `AllBe("EUR")`).

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | Lambda-parameternaam: eerste vrije uit `i`, `item`, `x`, `e`, `element`, `entry`, anders `itemN` (`LookupSymbols`) | Voorkomt conflict met locals/parameters |
| 2 | Selector-overload → Manual | Andere semantiek (eigenschap) |
| 3 | Niet-nullable waardetype als element → Manual | `i == null` compileert niet netjes; assertion is altijd waar |
| 4 | `AllBe` alleen met string-element en string-waarde | Enige FA-overload; `==` is ordinaal, gelijk aan FA |
| 5 | Verwachte waarde moet constant of opslag (`IsSafeToEvaluateTwice`) zijn, anders Manual | Expressieboom evalueert hem per element |
| 6 | Nullable subject → `ShouldNotBeNull()` ervoor (`FluentAssertionsSyntax.NotNull`) | Gelijk aan `fa.contain` |
| 7 | Proofs `all-be-*` in map `NotContainNulls/` | `ProofTests` koppelt map aan patroonklasse |
| 8 | Lege collectie: beide kanten slagen | Aanname uit API |

## Stappen

- [x] 1. Onderzoek
- [x] 2. Plan
- [x] 3. Pattern + registratie
- [x] 4. Proofs, `proofs-doc.py`
- [x] 5. Build/proofs groen, modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotContainNullsPattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotContainNulls/`
- `docs/projectinfo/patterns/fa-not-contain-nulls-{plan,modellen,verify}.md`, README-rij, `architectuur/proofs.md`

## Zie ook

- `docs/projectinfo/patterns/fa-not-contain-nulls-plan.md`
