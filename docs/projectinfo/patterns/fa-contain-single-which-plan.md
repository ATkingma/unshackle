---
kingmadoc: 1
feature: "fa-contain-single-which"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/
---

# Feature: fa.contain-single-which: ContainSingle becomes ShouldHaveSingleItem or ShouldContain(pred, 1)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-036.

## One-sentence summary

`x.Should().ContainSingle()` becomes `x.ShouldHaveSingleItem()`, `ContainSingle(pred)` becomes `x.ShouldContain(pred, 1)` and `var y = ….ContainSingle(pred).Which` becomes `var y = x.Where(pred).ShouldHaveSingleItem()`.

## Scope (in / out)

**In scope**

- `ContainSingle()` and `ContainSingle(predicate)` with an optional `because`.
- `.Which` as the only initializer of a declaration.

**Out of scope**

- `.Which.P.Should()...` chains (row `fa.which-chain`).
- Non-generic collections (Manual).

## Requirements

- **REQ-1**: WHEN `x.Should().ContainSingle()` is a statement THE SYSTEM SHALL give `x.ShouldHaveSingleItem()` (proof `simple`).
- **REQ-2**: WHEN `ContainSingle(pred)` is a statement THE SYSTEM SHALL give `x.ShouldContain(pred, 1)` (proofs `predicate`, `enumerable`).
- **REQ-3**: WHEN there is a `because` without becauseArgs THE SYSTEM SHALL pass it as customMessage (proof `because`).
- **REQ-4**: WHEN `var y = x.Should().ContainSingle([lambda]).Which` is used THE SYSTEM SHALL give `var y = x[.Where(lambda)].ShouldHaveSingleItem()`, with `using System.Linq` (proof `which`).
- **REQ-5**: WHEN the subject is nullable THE SYSTEM SHALL put `ShouldNotBeNull()` before it if the flow state is MaybeNull (proof `nullable`).
- **REQ-6**: IF the result is used further, the predicate is an Expression variable with `.Which`, becauseArgs or an AssertionScope THEN THE SYSTEM SHALL give Manual (proofs `which-chain`, `expression-variable`, `because-args`, `assertion-scope`).
- **Extension TAAK-096**: WHEN `.Which`/`.Subject` is used further (`….Which.Price`) THE SYSTEM SHALL replace `.Which` with the Shouldly call, which returns the item (proof `which-chain`); `.And` and other usage stay Manual. Replaces that part of REQ-6.
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args`); named arguments or a non-constant `because` stay Manual. Replaces that part of REQ-6.

## Assumptions

- Shouldly 4.3.0 (reflection): `ShouldHaveSingleItem<T>(IEnumerable<T>, customMessage)` returns T; `ShouldContain<T>(IEnumerable<T>, Expression<Func<T,bool>>, int expectedCount, customMessage)`.

## Risks

- The message text differs from FA; the behaviour (exactly one (match)) is the same.

## Models

Class diagram, sequence diagram and decision tree: [fa-contain-single-which-modellen.md](fa-contain-single-which-modellen.md).
