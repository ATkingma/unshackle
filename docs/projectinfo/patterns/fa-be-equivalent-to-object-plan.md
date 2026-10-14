---
kingmadoc: 1
feature: "fa-be-equivalent-to-object"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-15]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/
---
# Feature: fa.be-equivalent-to-object: obj.Should().BeEquivalentTo(other) becomes obj.ShouldBeEquivalentTo(other) or Manual

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-016. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.be-equivalent-to-object: `BeEquivalentTo` on an object only becomes `ShouldBeEquivalentTo` when both libraries demonstrably decide the same (same plain type, only simple members); everything else is Manual.

## Scope (in / out)

**In scope**

- `a.Should().BeEquivalentTo(b)` (with an optional `because` without `becauseArgs`) where `a` and `b` have the same compile-time type: a non-abstract class, struct or record without an `Equals` override, without a cycle and with at least one public member, whose reachable members are all primitives, `string`, `decimal`, enums, `DateTime`, `DateTimeOffset`, `Guid`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Nullable` of those, or such a plain type themselves.
- `Manual(reason)` for everything else.

**Out of scope**

- Strings: `fa.be-equivalent-to-string` (Find excludes `StringAssertions`).
- Rewriting `options =>` configuration or collections ourselves; that has no Shouldly equivalent.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `a.Should().BeEquivalentTo(b)` is on two values of the same plain type THE SYSTEM SHALL rewrite it to `a.ShouldBeEquivalentTo(b)` (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/simple.*.cs`, `new-expectation.*.cs`, `record.*.cs`, `nested.*.cs`).
- **REQ-2**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL pass it as the second argument (`customMessage`) (proof `.../because.*.cs`).
- **REQ-3**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `.../lambda.*.cs`).
- **REQ-4**: IF `NotBeEquivalentTo` is used THEN THE SYSTEM SHALL give Manual, because Shouldly has no `ShouldNotBeEquivalentTo` (proof `.../not-equivalent.*`).
- **REQ-5**: IF the assertion is a continuation of another assertion or the result is used THEN THE SYSTEM SHALL give Manual (proofs `.../and-chain.*`, `.../value-used.*`).
- **REQ-6** (replaced by REQ-14): IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `.../because-args.*`).
- **REQ-7**: IF an `options =>` configuration is passed THEN THE SYSTEM SHALL give Manual (proof `.../options.*`).
- **REQ-8**: IF subject and expectation have a different compile-time type (also an anonymous type or base versus derived class) THEN THE SYSTEM SHALL give Manual, because FluentAssertions only compares the members of the expectation and Shouldly requires equal types (proofs `.../anonymous.*`, `.../different-types.*`, `.../base-vs-derived.*`).
- **REQ-9**: IF the type is a collection or has a collection as a reachable member THEN THE SYSTEM SHALL give Manual, because FluentAssertions ignores the order by default and Shouldly does not (proofs `.../collection.*`, `.../collection-member.*`).
- **REQ-10**: IF the type or a reachable member is `object`, an interface, abstract or a BCL type outside the simple set THEN THE SYSTEM SHALL give Manual, because the runtime type determines the comparison (proofs `.../object-subject.*`, `.../object-member.*`).
- **REQ-11**: IF the type or a reachable member overrides `Equals` THEN THE SYSTEM SHALL give Manual, because FluentAssertions then uses `Equals` and Shouldly the members (proofs `.../equals-override.*`, `.../equals-member.*`).
- **REQ-12**: IF the type is part of a cyclic object graph THEN THE SYSTEM SHALL give Manual, because FluentAssertions fails and Shouldly passes (proof `.../cyclic.*`).
- **REQ-13**: IF the type has no public members THEN THE SYSTEM SHALL give Manual, because FluentAssertions throws an exception and Shouldly passes (proof `.../no-members.*`).
- **Extension TAAK-092**: WHEN subject and expectation are an `IEnumerable<T>` with the same leaf element type (primitives, string, enum, DateTime(Offset), Guid, … also nullable) THE SYSTEM SHALL give `x.ShouldBe(expected, ignoreOrder: true[, because])` (proofs `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal`, `AndChain/equivalent-collection`); another element type or non-leaf elements stay Manual (`collection-element-types`, `collection`). Probe: duplicates, null elements, empty, other length, null subject/expectation give the same verdict.
- **REQ-14** (Extension TAAK-097): WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage, also in the collection form (proofs `because-args`, `collection-because-args`); named arguments or a non-constant `because` stay Manual.
- **REQ-15**: IF the `params` overload `BeEquivalentTo(params items)` is used on a collection THEN THE SYSTEM SHALL give Manual before the argument split (`RewriteBeforeArguments`), because the extra arguments are items and not becauseArgs (proof `edge-collection-params`).

## Assumptions

- Behaviour determined with a test program against FluentAssertions 7.2.2 and Shouldly 4.3.0 (see TAAK-016): Shouldly is stricter than FluentAssertions (order of collections, runtime types, derived members, `int` versus `long`, use of `Equals`), so there a rewritten test fails audibly. Only the reverse is dangerous (FA fails, Shouldly passes): an `Equals` override without equal behaviour, cyclic graphs and types without public members; those are Manual.
- For the same plain types with simple members both decide equally: case-sensitive strings, `null` versus empty string, enums, `decimal` (1.0 equal to 1.00), `DateTime` (Kind ignored), `double.NaN` equal, structs, records, private members ignored, public fields included.
- A non-sealed class can have a derived runtime type with extra members; FluentAssertions ignores those, Shouldly does not. That gives at most an audible extra failure and is accepted.
- Shouldly has `ShouldBeEquivalentTo(expected, customMessage)`; proven by the compile check of the after proofs.

## Risks

- Audible extra failure with a derived runtime type (see above).
- The error message gets "Additional Info:" instead of FA's "because" prefix.
- Members with their own `Equals` in a type outside the source (BCL) are considered non-simple and give Manual.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-equivalent-to-object-modellen.md](fa-be-equivalent-to-object-modellen.md).

## Open questions

- None.
