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

# Feature: fa.order: BeInAscendingOrder/BeInDescendingOrder becomes ShouldBeInOrder

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12              |

> Drawn up from TAAK-162.

## One-sentence summary

`x.Should().BeInAscendingOrder(p => p.K)` becomes `x.Select(p => p.K).ShouldBeInOrder(SortDirection.Ascending)` (and `Descending`); without a selector without `Select`; a string key gets `StringComparer.Ordinal`.

## Scope (in / out)

**In scope**

- `BeInAscendingOrder` and `BeInDescendingOrder` with no arguments or a selector lambda, with an optional `because`.

**Out of scope**

- Overloads with `IComparer<T>`, `Func<T,T,int>` or selector plus comparer (Manual).
- `NotBeIn...Order`, `ThenBeIn...Order`, `BeInOrder(SortOrder)`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeInAscendingOrder(p => p.K)` or `BeInDescendingOrder(...)` is a statement THE SYSTEM SHALL give `x.Select(p => p.K).ShouldBeInOrder(SortDirection.<direction>)` and add `using System.Linq` (proofs `selector-ascending`, `selector-descending`).
- **REQ-2**: WHEN there is no selector THE SYSTEM SHALL give `x.ShouldBeInOrder(SortDirection.<direction>)` (proof `no-selector`).
- **REQ-3**: WHEN there is a `because` THE SYSTEM SHALL give it as the third argument after a `null` comparer, with becauseArgs via `string.Format(InvariantCulture, ...)` (proofs `because`, `because-args`).
- **REQ-4**: WHEN the key (or the element) is a `string` THE SYSTEM SHALL pass `System.StringComparer.Ordinal` as the comparer (proofs `string-key`, `string-elements`, `string-because`).
- **REQ-5**: WHEN the subject is MaybeNull in a nullable context THE SYSTEM SHALL put `ShouldNotBeNull()` before it (proof `nullable`).
- **REQ-6**: IF a comparer, comparison delegate, selector plus comparer, or a selector that is not a lambda THEN THE SYSTEM SHALL give Manual (proofs `comparer`, `comparison`, `selector-comparer`, `expression-variable`).
- **REQ-7**: IF inside an AssertionScope or the result is used THEN THE SYSTEM SHALL give Manual (proofs `assertion-scope`, `result-used`).

## Assumptions

- Probe (Shouldly 4.3.0 against FA 7.2.2): equal neighbours, empty and 1-element sequences, null elements at the front and a null subject (both fail) behave the same.
- Probe: FA compares strings ordinally (`"a","B"` fails, `"Z","Å"` passes); Shouldly's default is culture-sensitive. Hence the Ordinal comparer. Tuples with strings and other types use the default comparer in both.

## Risks

- A key that compares strings internally via its own `IComparable` is equal in both; only a bare `string` is corrected.
- A null subject gives a `NullReferenceException`/`ArgumentNullException` in Shouldly instead of an assertion; the test fails in both cases.

## Models

Class diagram, sequence diagram and decision tree: [fa-order-modellen.md](fa-order-modellen.md).
