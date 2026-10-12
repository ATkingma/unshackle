---
kingmadoc: 1
feature: "fa-order"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/Order/
---

# Feature: fa.order: BeInAscendingOrder/BeInDescendingOrder wordt ShouldBeInOrder

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12              |

> Opgesteld uit TAAK-162.

## One-sentence summary

`x.Should().BeInAscendingOrder(p => p.K)` wordt `x.Select(p => p.K).ShouldBeInOrder(SortDirection.Ascending)` (en `Descending`); zonder selector zonder `Select`; een string-sleutel krijgt `StringComparer.Ordinal`.

## Scope (in / out)

**In scope**

- `BeInAscendingOrder` en `BeInDescendingOrder` met geen argumenten of een selector-lambda, met optionele `because`.

**Out of scope**

- Overloads met `IComparer<T>`, `Func<T,T,int>` of selector plus comparer (Manual).
- `NotBeIn...Order`, `ThenBeIn...Order`, `BeInOrder(SortOrder)`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeInAscendingOrder(p => p.K)` of `BeInDescendingOrder(...)` als statement staat THE SYSTEM SHALL `x.Select(p => p.K).ShouldBeInOrder(SortDirection.<richting>)` geven en `using System.Linq` toevoegen (proofs `selector-ascending`, `selector-descending`).
- **REQ-2**: WHEN er geen selector is THE SYSTEM SHALL `x.ShouldBeInOrder(SortDirection.<richting>)` geven (proof `no-selector`).
- **REQ-3**: WHEN `because` staat THE SYSTEM SHALL die als derde argument geven na een `null`-comparer, bij becauseArgs via `string.Format(InvariantCulture, ...)` (proofs `because`, `because-args`).
- **REQ-4**: WHEN de sleutel (of het element) een `string` is THE SYSTEM SHALL `System.StringComparer.Ordinal` als comparer meegeven (proofs `string-key`, `string-elements`, `string-because`).
- **REQ-5**: WHEN het subject in een nullable context MaybeNull is THE SYSTEM SHALL `ShouldNotBeNull()` ervoor zetten (proof `nullable`).
- **REQ-6**: IF een comparer, comparison-delegate, selector plus comparer, of een selector die geen lambda is THEN THE SYSTEM SHALL Manual geven (proofs `comparer`, `comparison`, `selector-comparer`, `expression-variable`).
- **REQ-7**: IF in een AssertionScope of het resultaat wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `assertion-scope`, `result-used`).

## Assumptions

- Probe (Shouldly 4.3.0 tegen FA 7.2.2): gelijke buren, lege en 1-element-reeksen, null-elementen vooraan en een null-subject (beide falen) gedragen zich gelijk.
- Probe: FA vergelijkt strings ordinaal (`"a","B"` faalt, `"Z","Å"` slaagt); Shouldly's standaard is cultuurgevoelig. Daarom de Ordinal-comparer. Tuples met strings en andere typen gebruiken bij beide de standaardcomparer.

## Risks

- Een sleutel die intern strings vergelijkt via een eigen `IComparable` is bij beide gelijk; alleen een kale `string` wordt gecorrigeerd.
- Een null-subject geeft bij Shouldly een `NullReferenceException`/`ArgumentNullException` in plaats van een assertion; de test faalt in beide gevallen.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-order-modellen.md](fa-order-modellen.md).
