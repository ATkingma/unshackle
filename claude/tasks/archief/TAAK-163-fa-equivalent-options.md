# TAAK-163 — Patroon fa.equivalent-options

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: `BeEquivalentTo(y, o => o.Option())` zo exact mogelijk automatisch; anders Manual met een reden per optie.

## Context voor de volgende agent

- Rij 32 in `claude/tasks/patroon-queue.md`. Voorheen gaf `fa.be-equivalent-to-object` voor elke options-aanroep dezelfde Manual ("options => ...").

## Analyse

Probe (scratchpad `probe163`, FA 7.2.2 vs Shouldly 4.3.0, 45 gevallen): `WithStrictOrdering` op `List<T>` vs `T[]` en `ShouldBe(y)` geven dezelfde uitkomst voor leaf-items (int, string met null-items, double met NaN, enum, int?, DateTime, decimal), sealed records, record structs en structs met leaf-leden: gelijk, omgekeerd, langer/korter, duplicaten, leeg, null-subject/-expectation/-beide, because. De laatste van `WithStrictOrdering`/`WithoutStrictOrdering` wint. `ExcludingMissingMembers`, `ThrowingOnMissingMembers` en `WithStrictOrdering` op een object gedragen zich gelijk aan "geen optie" bij hetzelfde compile-time type. Shouldly heeft voor `ShouldBe` met message alleen `(expected, ignoreOrder, customMessage)`.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | Eigen klasse `EquivalentOptionsPattern : BeEquivalentToObjectPattern`; basis-`Find` slaat `BeEquivalentTo` met options over | Eén bezitter per node; hergebruik van de object- en leaf-collectielogica |
| 2 | Alleen een inline lambda `o => o.A().B()` wordt gelezen | Blok, variabele, method group: niet te controleren, Manual |
| 3 | `WithoutStrictOrdering`, `ExcludingMissingMembers`, `ThrowingOnMissingMembers` vervallen; `WithStrictOrdering` op een niet-collectie ook | Geprobeerd: geen effect bij hetzelfde type |
| 4 | Strikt op collectie met gelijk itemtype en waarde-items → `ShouldBe(y)` (met because: `ignoreOrder: false, because`) | Geprobeerd gelijk; bestaande vorm uit `fa.equal-sequence` |
| 5 | Waarde-items: leaf, sealed record, record struct, struct met alleen leaf-velden (publiek of achter publieke auto-property), geen eigen `Equals`, geen berekende publieke property | Dan is `Equals` gelijk aan FA's ledenvergelijking; class, niet-sealed record, collectie-lid: Manual |
| 6 | `ExcludingMissingMembers` + verschillende types → eigen Manual-reden | Dat is het gebruikelijke geval van die optie |
| 7 | Overige opties: Manual, reden per categorie (selectie, vergelijking, runtime-types, cycli, tracing) met de optietekst erin | Gevraagd: specifieke reden per optie |
| 8 | `options.*` proof uit `BeEquivalentToObject/` verhuisd naar `EquivalentOptions/excluding` | Het patroon vindt die aanroep niet meer |

## Stappen

- [x] 1. Onderzoek + probe
- [x] 2. Plan
- [x] 3. `EquivalentOptionsPattern`, registratie, basispatroon aangepast
- [x] 4. 38 proofs, `proofs-doc.py`
- [x] 5. Build en tests groen (35 bekende CRLF-falers), modellen, verify, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs` (nieuw), `BeEquivalentToObjectPattern.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/`; `BeEquivalentToObject/options.*` verwijderd
- `docs/projectinfo/patterns/fa-equivalent-options-{plan,modellen,verify}.md`, README-rij, `architectuur/proofs.md`

## Zie ook

- `claude/tasks/archief/TAAK-016-fa-be-equivalent-to-object.md`
- `docs/projectinfo/patterns/fa-equivalent-options-plan.md`
