---
kingmadoc: 1
feature: "fa-equivalent-options"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/
---

# Feature: fa.equivalent-options: BeEquivalentTo with options

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by hand (kingmadoc missing) |

> Drawn up from TAAK-163.

## One-sentence summary

`x.Should().BeEquivalentTo(y, o => o.Option())` becomes `ShouldBe`/`ShouldBeEquivalentTo` where that is exactly possible (options that change nothing are dropped, `WithStrictOrdering` on value collections becomes `ShouldBe(y)`), otherwise Manual with a reason per option.

## Scope (in / out)

**In scope**

- `BeEquivalentTo(expected, Func<EquivalencyAssertionOptions<T>, ...> config, because, becauseArgs)` on object and collection assertions.
- An inline lambda `o => o.A().B()`; the last `WithStrictOrdering`/`WithoutStrictOrdering` wins (as in FA).

**Out of scope**

- `NotBeEquivalentTo` with options (stays Manual in `fa.be-equivalent-to-object`).
- `StringAssertions.BeEquivalentTo(.., o => o.IgnoringCase())` (lives in `fa.be-equivalent-to-string`).
- Options in a block body, variable or method group (Manual).

## Requirements

- **REQ-1**: WHEN `WithStrictOrdering()` (last applicable) is on a collection of values (leaf, sealed record, record struct or struct with only leaf fields) with an equal item type THE SYSTEM SHALL give `x.ShouldBe(y)`, with `because`: `x.ShouldBe(y, ignoreOrder: false, because)` (proofs `strict-ordering`, `-lambda-parenthesized`, `-because`, `-because-args`, `-strings`, `-record`, `-record-struct`, `-struct`, `-with-missing-members`, `without-then-strict`).
- **REQ-2**: WHEN only `WithoutStrictOrdering()`, `ExcludingMissingMembers()` or `ThrowingOnMissingMembers()` (or `WithStrictOrdering()` on a non-collection) is used THE SYSTEM SHALL drop the options and give the form of `fa.be-equivalent-to-object`: `ShouldBeEquivalentTo(y)` or `ShouldBe(y, ignoreOrder: true)` (proofs `without-strict-ordering`, `strict-then-without`, `strict-ordering-object`, `excluding-missing-members`, `throwing-on-missing-members`, `lenient-collection-ints`).
- **REQ-3**: IF `WithStrictOrdering()` is on a collection with items that are a class, a non-sealed record or a record with a computed/collection member, or with different item types THEN THE SYSTEM SHALL give Manual (proofs `strict-ordering-class`, `-record-unsealed`, `-record-computed`, `-record-list-member`, `-item-types`).
- **REQ-4**: IF a member selection, comparison, type, cycle or trace option (`Excluding`, `Including`, `Using`, `ComparingByValue`, `RespectingRuntimeTypes`, `WithTracing`, ...) is used THEN THE SYSTEM SHALL give Manual with a reason per option that names the option text (proofs `excluding`, `excluding-with-strict`, `excluding-two`, `including`, `using-comparer`, `comparing-by-value`, `respecting-runtime-types`, `with-tracing`, `collection-excluding`).
- **REQ-5**: IF `ExcludingMissingMembers()` is used with different compile-time types THEN THE SYSTEM SHALL give Manual with that reason (proof `excluding-missing-members-other-type`).
- **REQ-6**: IF the options are not an inline lambda of the form `o => o.Option(...)` THEN THE SYSTEM SHALL give Manual (proofs `block-body`, `options-variable`).

## Assumptions

- Probe against FA 7.2.2 and Shouldly 4.3.0 (scratchpad `probe163`, 45 cases): for leaf collections, sealed records, record structs and structs with leaf members, `WithStrictOrdering` and `ShouldBe(y)` give the same outcome for equal, other order, other length, duplicates, null items, null subject/expectation, NaN, DateTime kind and decimal scale.
- `ExcludingMissingMembers`/`ThrowingOnMissingMembers` do nothing with the same compile-time type (tried); `WithStrictOrdering` on an object without collections neither (collections give Manual in the base pattern).

## Risks

- A struct or record with a private auto-property, a computed public property or its own `Equals` is deliberately not recognised (Manual): FA compares members, `ShouldBe` compares with `Equals`.
- `WithTracing` falls under Manual because it changes the error message, not the outcome.

## Models

Class diagram, sequence diagram and decision tree: [fa-equivalent-options-modellen.md](fa-equivalent-options-modellen.md).

## See also

- [fa-equivalent-options-verify.md](fa-equivalent-options-verify.md)
- [fa-be-equivalent-to-object-plan.md](fa-be-equivalent-to-object-plan.md)
- `claude/tasks/archief/TAAK-163-fa-equivalent-options.md`
