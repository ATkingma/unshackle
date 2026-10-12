# TAAK-162 — Patroon fa.order

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: `x.Should().BeInAscendingOrder(p => p.K)` / `BeInDescendingOrder` → `x.Select(p => p.K).ShouldBeInOrder(SortDirection.Ascending)`.

## Context voor de volgende agent

- Rij `fa.order` in `claude/tasks/patroon-queue.md`. Sample: `AdvancedAssertionTests` gebruikt `BeInAscendingOrder(p => p.Sku)` (string-sleutel).

## Analyse

FA 7.2.2 (`GenericCollectionAssertions`): `BeInAscendingOrder()`, `(Expression<Func<T,K>>)`, `(IComparer<T>)`, `(Func<T,T,int>)`, `(Expression, IComparer<K>)`, elk met because/becauseArgs; idem Descending. Shouldly 4.3.0: `ShouldBeInOrder(SortDirection, IComparer<T>? = null, string? customMessage = null)`. Probe (scratchpad `probe162/`): gelijke buren, lege/1-element-reeks en null-elementen vooraan gedragen zich gelijk; een null-subject faalt bij beide. **Verschil**: FA vergelijkt strings ordinaal (`"a","B"` faalt, `"Z","Å"` slaagt), Shouldly's standaard is cultuurgevoelig (omgekeerd). Met `StringComparer.Ordinal` zijn ze gelijk (probe). Tuples met strings: beide standaardcomparer, gelijk.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | Selector-lambda → `x.Select(sel).ShouldBeInOrder(dir)`, `System.Linq` toegevoegd | Shouldly heeft geen selector-overload |
| 2 | String-sleutel of -element → comparer `System.StringComparer.Ordinal` | Gedragsverschil culture/ordinal (probe) |
| 3 | because → derde argument; zonder string-comparer een `null`-placeholder | customMessage staat na de comparer |
| 4 | IComparer/Func-comparison/selector+comparer → Manual | Opdracht; typeverschil `IComparer<T>` van element/sleutel |
| 5 | Selector die geen lambda is (Expression-variabele) → Manual | `Select` wil een Func |
| 6 | Nullable subject → `ShouldNotBeNull()` via `NotNullReceiver` | Gelijk aan andere patronen |
| 7 | Null-subject: geen aanpassing | Faalt bij beide (NRE/ArgumentNull in plaats van assertion) |

## Stappen

- [x] 1. Onderzoek + probe
- [x] 2. Plan
- [x] 3. `OrderPattern` + registratie
- [x] 4. Proofs in `Order/`, `proofs-doc.py`
- [x] 5. Build/proofs groen, modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs` (nieuw), `FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/Order/`
- `docs/projectinfo/patterns/fa-order-{plan,modellen,verify}.md`, README-rij, `architectuur/proofs.md`

## Zie ook

- `docs/projectinfo/patterns/fa-order-plan.md`
- `claude/tasks/archief/TAAK-160-fa-not-equal.md`
