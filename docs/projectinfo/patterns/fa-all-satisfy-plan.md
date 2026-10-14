---
kingmadoc: 1
feature: "fa-all-satisfy"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AllSatisfyPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/
---
# Feature: fa.all-satisfy: x.Should().AllSatisfy(a => ...) becomes an emptiness check plus a foreach

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-014.

## One-sentence summary

fa.all-satisfy: `x.Should().AllSatisfy(a => body)` becomes `x.ShouldNotBeEmpty(); foreach (var a in x) { body }`, because `AllSatisfy` takes an `Action<T>` with assertions while Shouldly's `ShouldAllBe` only takes a bool predicate; unsafe forms become Manual.

## Scope (in / out)

**In scope**

- A standalone `x.Should().AllSatisfy(lambda)` as a statement, with an inline lambda with one parameter (expression or block body, also with a typed parameter), directly in a block; a subject with side effects is hoisted first (TAAK-098).
- `Manual(reason)` for `because`/`becauseArgs`, method group, `return` in the body, async lambda, result used, `.And` continuation and a statement outside a block.

**Out of scope**

- Rewriting the assertions in the body: the other patterns do that (this pattern is first in `FluentAssertionsFix`, so those patterns still see the body as separate statements).
- `SatisfyRespectively`, `OnlyContain` (own patterns).

## Requirements

- **REQ-1**: WHEN `x.Should().AllSatisfy(i => body)` is a statement THE SYSTEM SHALL replace it with `x.ShouldNotBeEmpty();` and `foreach (var i in x) { body; }` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/simple.*.cs`).
- **REQ-2**: WHEN the lambda is a block THE SYSTEM SHALL take over the block as the loop body with the correct indentation (proof `block.*.cs`).
- **REQ-3**: WHEN the lambda parameter has a type THE SYSTEM SHALL use that type in the `foreach` (proof `typed-parameter.*.cs`).
- **REQ-4**: WHEN the subject is a member access THE SYSTEM SHALL rewrite it (proof `member-subject.*.cs`).
- **REQ-5**: IF the assertion has a `because` THEN THE SYSTEM SHALL give Manual, because the message cannot go into the loop (proof `because.*`).
- **REQ-6**: IF `becauseArgs` or named arguments are used THEN THE SYSTEM SHALL give Manual (proof `because-args.*`).
- **REQ-7**: IF the argument is not an inline lambda (method group) THEN THE SYSTEM SHALL give Manual (proof `method-group.*`).
- **REQ-8**: IF the body contains a `return`, or the lambda is async THEN THE SYSTEM SHALL give Manual (proofs `return.*`, `async.*`).
- **REQ-9** (replaced by REQ-13): IF the subject is not a name or member access THEN THE SYSTEM SHALL give Manual, because it would be evaluated twice (proof `subject-call.*`).
- **REQ-10**: IF the result is used or the assertion follows `.And` THEN THE SYSTEM SHALL give Manual (proofs `used.*`, `and-chain.*`).
- **REQ-11**: IF the statement is not directly in a block THEN THE SYSTEM SHALL give Manual (proof `not-in-block.*`).
- **REQ-12**: WHEN the body contains a FluentAssertions assertion (sample `OrderHandlerTests`) THE SYSTEM SHALL produce, after the whole Fix, a compilable loop with a Shouldly assertion (integration test on `samples/`, checked manually with `fix` on a copy).
- **REQ-13** (Extension TAAK-098): WHEN the subject is not safe to evaluate twice (e.g. a call) and the statement is directly in a block THE SYSTEM SHALL hoist the subject to `var subject = x;` before `ShouldNotBeEmpty()` and the loop, instead of Manual (replaces REQ-9; proof `subject-call.*.cs`).

## Assumptions

- FluentAssertions 7.2.2 fails on an empty and on a `null` collection (tested); `ShouldNotBeEmpty()` before the loop preserves that.
- A `foreach` stops at the first failed assertion; FA collects all failed elements into one message. Documented difference.
- Property getters in the subject are pure (same assumption as `fa.be-greater-less`).

## Risks

- The failure message changes (first failure instead of all failures per index).
- A body with FluentAssertions assertions that are not yet rewritten (Manual elsewhere) leaves FA code in the loop.

## Models

Class diagram, sequence and decision tree: [fa-all-satisfy-modellen.md](fa-all-satisfy-modellen.md).

## Open questions

- None.
