# TAAK-038 — Patroon fa.equal-sequence

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().Equal(seq)` / `Equal(params)` → `x.ShouldBe(seq)` (volgorde-gevoelig).

## Context voor de volgende agent

- Rij 21 in `claude/tasks/patroon-queue.md` (ronde 2).

## Analyse

FA 7.2.2: `GenericCollectionAssertions.Equal(params T[])`, `Equal(IEnumerable<T>, because, becauseArgs)`, `Equal(IEnumerable<TExp>, Func<T,TExp,bool>, …)`, `NotEqual(IEnumerable<T>, …)`; `StringCollectionAssertions.Equal(params string[])`/`Equal(IEnumerable<string>)`; `GenericDictionaryAssertions.Equal(T)` (volgorde-ongevoelig). Shouldly 4.3.0: `ShouldBe<T>(IEnumerable<T>, IEnumerable<T>, bool ignoreOrder = false)` en `(…, bool ignoreOrder, string? customMessage)`; **geen** sequence-`ShouldNotBe`. Semantiek gecontroleerd met een probe-project (FA 7.2.2 + Shouldly 4.3.0) in de scratchpad (`semprobe3/`). Uitkomst: beide volgorde-gevoelig, ongelijke lengte faalt, null-subject faalt bij beide, strings hoofdlettergevoelig.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | Params-vorm → `new T[] { .. }` met het FA-elementtype | `new[] {1}` op `List<long>` zou `int[]` geven en de type-inferentie breken |
| 2 | `because` → `ShouldBe(y, ignoreOrder: false, because)` | Enige sequence-overload met message; named arg maakt de `false` leesbaar |
| 3 | `NotEqual` → Manual | Shouldly heeft geen `ShouldNotBe` voor sequences; generieke `ShouldNotBe<T>` vraagt een expliciet type en slaagt op null-subject (FA faalt) |
| 4 | Dictionary en predicate → Manual | Dictionary-`Equal` is volgorde-ongevoelig; predicate vraagt een eigen comparer |
| 5 | Geen `NotNull(subject)` | Shouldly's `ShouldBe` accepteert null en faalt zelf, zoals FA |

## Stappen

- [x] 1. Taak, onderzoek (reflectie + semantiek-probe), keuzes
- [x] 2. KingmaDoc-plan
- [x] 3. `EqualSequencePattern` + registratie in `s_assertions` (ook schakel van `fa.and-chain`)
- [x] 4. Proofs in `EqualSequence/` + `AndChain/equal-length-approximately`
- [x] 5. Build/test groen (474 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/`
- `docs/projectinfo/patterns/fa-equal-sequence-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-035-fa-string-start-end.md`
- `docs/projectinfo/patterns/fa-equal-sequence-plan.md`
