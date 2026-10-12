# TAAK-160 — Patroon fa.not-equal

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: `x.Should().NotEqual(seq)` → `x.ShouldNotBe(seq)` (volgorde-gevoelig).

## Context voor de volgende agent

- Rij `fa.not-equal` in `claude/tasks/patroon-queue.md`. Voorheen een Manual-geval in `fa.equal-sequence` (TAAK-038); nu eigen patroon.

## Analyse

FA 7.2.2: `GenericCollectionAssertions.NotEqual(IEnumerable<T>, because, becauseArgs)`, `StringCollectionAssertions.NotEqual`, `GenericDictionaryAssertions.NotEqual(T)` (volgorde-ongevoelig). Shouldly 4.3.0: geen sequence-overload, maar `ShouldNotBe<T>(T, T)` vergelijkt `IEnumerable` element-voor-element in volgorde. Probe (Shouldly 4.3.0, scratchpad `probe/`): zelfde reeks faalt; omgekeerde volgorde en andere lengte slagen; twee `List<int>` met dezelfde inhoud falen. **Verschil**: een null-subject slaagt bij Shouldly, terwijl FA's `NotEqual` faalt op null, daarom de guard (keuze 5).

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | Zelfde type links/rechts → `x.ShouldNotBe(y)` | T wordt afgeleid |
| 2 | Verschillend type (`List<int>` vs `int[]`) → `x.ShouldNotBe<IEnumerable<T>>(y)` met T uit de FA-parameter | Anders faalt de inferentie |
| 3 | `because` → `ShouldNotBe(y, because)`; becauseArgs via `string.Format(InvariantCulture, …)` | `ShouldNotBe<T>(T, T, string?)` bestaat |
| 4 | Dictionary → Manual | FA vergelijkt volgorde-ongevoelig |
| 5 | `x.ShouldNotBeNull().ShouldNotBe(y)` tenzij de flow-state NotNull is (ook zonder nullable-context) | Migratie mag niet zwakker worden: Shouldly slaagt op null, FA faalt (coördinator-eis) |
| 6 | `NotEqual` uit `EqualSequencePattern` gehaald | Dat patroon gaf Manual; proofs `not-equal.*` verhuisd naar `NotEqual/` |

## Stappen

- [x] 1. Onderzoek + probe
- [x] 2. Plan
- [x] 3. `NotEqualPattern` + registratie, `EqualSequencePattern` ontdaan van NotEqual
- [x] 4. Proofs in `NotEqual/`, `proofs-doc.py`
- [x] 5. Build/proofs groen, modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs` (nieuw), `EqualSequencePattern.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotEqual/`; `EqualSequence/not-equal.*` verwijderd
- `docs/projectinfo/patterns/fa-not-equal-{plan,modellen,verify}.md`, README-rij, `architectuur/proofs.md`

## Zie ook

- `claude/tasks/archief/TAAK-038-fa-equal-sequence.md`
- `docs/projectinfo/patterns/fa-not-equal-plan.md`
