---
kingmadoc: 1
feature: "fa-equal-sequence"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/
---

# Feature: fa.equal-sequence: Equal(seq) and Equal(params) become ShouldBe(seq), order-sensitive

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-038.

## One-sentence summary

`x.Should().Equal(y)` becomes `x.ShouldBe(y)`; `Equal(1, 2)` becomes `x.ShouldBe(new T[] { 1, 2 })`; with `because` `x.ShouldBe(y, ignoreOrder: false, because)`.

## Scope (in / out)

**In scope**

- `Equal(IEnumerable<T>)`, `Equal(params T[])` on `GenericCollectionAssertions` and `StringCollectionAssertions`, with an optional `because`.

**Out of scope**

- `NotEqual` (own pattern `fa.not-equal` since TAAK-160).
- Dictionary `Equal` with keys/values that are not a leaf, record or struct of leaves, or with an expectation that is not a dictionary (Manual).
- `Equal(seq, predicate)` (Manual).

## Requirements

- **REQ-1**: WHEN `x.Should().Equal(y)` is a statement THE SYSTEM SHALL give `x.ShouldBe(y)` (proofs `simple`, `params-array`).
- **REQ-2**: WHEN `Equal(a, b, ...)` with separate params is used THE SYSTEM SHALL give `x.ShouldBe(new T[] { a, b })` with T the element type of FA (proof `params`).
- **REQ-3**: WHEN there is a `because` without becauseArgs THE SYSTEM SHALL give `ShouldBe(y, ignoreOrder: false, because)` (proof `because`).
- **REQ-4**: WHEN the subject is nullable THE SYSTEM SHALL rewrite without a null check; Shouldly fails on null itself (proof `nullable`).
- **REQ-5**: IF a dictionary, a predicate, becauseArgs, an AssertionScope or `.And` THEN THE SYSTEM SHALL give Manual (proofs `dictionary`, `predicate`, `because-args`, `assertion-scope`, `and`).
- **REQ-6**: WHEN `Equal` is a link after `.And` THE SYSTEM SHALL give a separate `ShouldBe` statement via `fa.and-chain` (proof `AndChain/equal-length-approximately`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `ShouldBe(y, ignoreOrder: false, string.Format(CultureInfo.InvariantCulture, because, args))` (Fixed, proof `because-args`); named arguments or a non-constant `because` stay Manual. REQ-5 therefore no longer applies to becauseArgs.
- **Extension TAAK-225**: WHEN `Equal(y)` is on a dictionary, y is also a dictionary with the same `KeyValuePair<K, V>` and K and V are value items (`IsValueLike`: leaf, or record/struct of leaves) THE SYSTEM SHALL give `x.ShouldBe(y, ignoreOrder: true)` (with `because`: as the third argument) (proofs `dictionary`, `dictionary-because`); other key/value types or a list of pairs stay Manual (proofs `dictionary-class-values`, `dictionary-pair-list`). Behaviour probed with FA 7.2.2 and Shouldly 4.3.0: order, missing/extra key, another value, `null` values, empty, immutable/read-only subject equal; only `null` as both subject and expectation differs (FA throws, Shouldly passes). REQ-5 therefore no longer applies to these dictionaries.

## Assumptions

- Semantics checked with a probe (FA 7.2.2 + Shouldly 4.3.0): both order-sensitive, element by element, unequal length fails, a null subject fails in both.
- Shouldly 4.3.0: `ShouldBe<T>(IEnumerable<T>, IEnumerable<T>, bool ignoreOrder = false)` and `(…, bool ignoreOrder, string? customMessage)`; no `ShouldNotBe` for sequences.

## Risks

- Strings in the collection: FA and Shouldly both compare case-sensitively (probe `strs`).

## Models

Class diagram, sequence diagram and decision tree: [fa-equal-sequence-modellen.md](fa-equal-sequence-modellen.md).
