---
kingmadoc: 1
feature: "fa-be-equivalent-to-collection"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/ParamsArraySyntax.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/
---

# Feature: fa.be-equivalent-to-object: BeEquivalentTo on a collection becomes ShouldBe(ignoreOrder: true)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by hand (kingmadoc missing) |

> Drawn up from TAAK-172 (corpus run round 3: 207 Manuals "FluentAssertions ignores the item order by default").

## One-sentence summary

`x.Should().BeEquivalentTo(expected)` and `x.Should().BeEquivalentTo("a", "b")` on a collection of value items (leaf, sealed record, struct) become `x.ShouldBe(expected, ignoreOrder: true)`; other collections stay Manual.

## Scope (in / out)

**In scope**

- Extension of the collection case of `BeEquivalentToObjectPattern` (TAAK-092, only leaf items) to `IsValueLike` items, the same check as `fa.contain-equivalent-of`.
- The params form `BeEquivalentTo("a", "b")` (only `StringCollectionAssertions` has it) becomes `ShouldBe(new string[] { "a", "b" }, ignoreOrder: true)`, with the formatting of `ParamsArraySyntax` (shared with `fa.equal-sequence`, TAAK-153).

**Out of scope**

- Classes without value equality, non-sealed records, records with nested members, an expectation type other than the element type, options lambdas: Manual.

## Requirements

- **REQ-1**: WHEN subject and expectation are collections of the same leaf element type THE SYSTEM SHALL give `ShouldBe(expected, ignoreOrder: true[, because])` (existing proofs `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal`).
- **REQ-2**: WHEN the element is a sealed record, record struct or struct with leaf members THE SYSTEM SHALL give the same rewrite (proofs `collection-record`, `collection-struct`).
- **REQ-3**: WHEN the params form `BeEquivalentTo("a", "b")` is on `IEnumerable<string>` THE SYSTEM SHALL give `ShouldBe(new string[] { ... }, ignoreOrder: true)`, also empty, with `null` items and over several lines keeping comments (proofs `params-strings`, `edge-collection-params`, `params-multi-line`).
- **REQ-4**: WHEN an array is passed as the argument THE SYSTEM SHALL treat it as an ordinary expectation (proof `params-array`).
- **REQ-5**: IF the element is a class, a non-sealed record or a record with nested members, or the expectation's element differs from the subject's THEN THE SYSTEM SHALL give Manual with a specific reason (proofs `collection`, `collection-record-unsealed`, `collection-record-nested`, `collection-element-types`).

## Assumptions

- Tried against FluentAssertions 7.2.2 and Shouldly 4.3.0: reordering, duplicate items (the count matters), another count, case-sensitive strings, `null` items, empty collections, a null subject, structs and record structs give the same outcome on both sides.
- Only `StringCollectionAssertions` has a `params` overload; for `IEnumerable<int>` or records `BeEquivalentTo(1, 2)` does not compile.

## Risks

- `BeEquivalentTo("ab")` on an `IEnumerable<char>` subject binds to the `IEnumerable<char>` overload (string is `IEnumerable<char>`); the existing collection check treats that as an ordinary expectation.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-equivalent-to-collection-modellen.md](fa-be-equivalent-to-collection-modellen.md).

## See also

- [fa-be-equivalent-to-collection-verify.md](fa-be-equivalent-to-collection-verify.md)
- [fa-be-equivalent-to-object-plan.md](fa-be-equivalent-to-object-plan.md)
- `claude/tasks/archief/TAAK-172-be-equivalent-to-collectie-ignore-order.md`
