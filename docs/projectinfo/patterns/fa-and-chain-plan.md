---
kingmadoc: 1
feature: "fa-and-chain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/
---

# Feature: fa.and-chain: x.Should().A().And.B() becomes one Shouldly statement per link

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-033.

## One-sentence summary

fa.and-chain: `x.Should().A().And.B();` is split per link and each link is rewritten by its own pattern (Composite), without duplicating logic.

## Scope (in / out)

**In scope**

- Statement chains with `.And`; a subject with side effects is hoisted first (TAAK-098).
- Every link that an existing assertion pattern makes Fixed, including `because`, nullable subjects and multiline chains.
- Manual for AssertionScope, used result (`.Which`, assignment), not in a block, an unsupported or Manual link.

**Out of scope**

- `.Which` chains (row `fa.which-chain`).
- ~~Putting the subject in a temp variable once~~: done since TAAK-098, only for subjects with side effects.

## Requirements

- **REQ-1**: WHEN `x.Should().A().And.B()` is a statement THE SYSTEM SHALL give `x.ShouldA(); x.ShouldB();` via the link patterns (proofs `simple`, `three-links`, `be-of-type`, `edge-multiline`).
- **REQ-2**: WHEN a link has `because` THE SYSTEM SHALL pass it along per statement (proof `because`).
- **REQ-3**: WHEN the subject is a member access or nullable THE SYSTEM SHALL apply the link rules (proofs `member-subject`, `nullable`).
- **REQ-4** (replaced by REQ-8): IF the subject is a call or indexer THEN THE SYSTEM SHALL give Manual (proofs `subject-call`, `subject-indexer`).
- **REQ-5**: IF a link is rewritten by no pattern or gives Manual THEN THE SYSTEM SHALL give Manual for the whole chain (proof `unsupported-link`; a Manual link gives "Link X of the .And chain: reason", no proof of its own anymore).
- **REQ-6**: IF the result is used or the statement is not directly in a block THEN THE SYSTEM SHALL give Manual (proofs `used`, `which`, `no-block`).
- **REQ-7**: IF the chain is in an AssertionScope THEN THE SYSTEM SHALL give Manual (proof `assertion-scope`).
- **REQ-8** (Extension TAAK-098): WHEN the subject is not safe to evaluate twice (call, indexer) THE SYSTEM SHALL hoist the subject to `var subject = x;` (several in one member: `subject2`, …) and then split, instead of Manual (proofs `subject-call`, `subject-indexer`, `subject-call-twice`).
- **REQ-9** (Extension TAAK-092..097): WHEN a link is made Fixed by a newer or extended pattern THE SYSTEM SHALL include that link, also with `becauseArgs` (proofs `be-same-as`, `dates`, `equal-length-approximately`, `equivalent-collection`, `greater-less`, `link-because-args`).
- **REQ-10** (Extension TAAK-171): WHEN `x.Should().ContainKeys(a, b)` or `NotContainKeys(a, b)` (params form) is a statement or link THE SYSTEM SHALL split it into one `ContainKey`/`NotContainKey` per key and let `ContainKeyPattern` rewrite those, also for `IReadOnlyDictionary` (proofs `contain-keys`, `contain-keys-and`, `not-contain-keys`, `contain-keys-read-only`, `contain-keys-subject-call`).
- **REQ-11** (TAAK-171): WHEN `ContainValue(s)` or `NotContainValue(s)` is on a type with a `Values` property THE SYSTEM SHALL split per value into `x.Values.Should().Contain(v)` (resp. `NotContain`), with `because` for the singular form, and rewrite `ContainKeys(..).And.ContainValues(..)` in one go (proofs `contain-value`, `contain-value-because`, `not-contain-value`, `contain-value-read-only`, `contain-values`, `not-contain-values`, `contain-keys-values`).
- **REQ-12** (TAAK-171): WHEN `x.Should().ContainKey(k).WhoseValue.Should().M(..)` (optionally with `.And`) is a statement, x has an indexer on the key and k is a constant or an expression that is safe to repeat THE SYSTEM SHALL give `x.ShouldContainKey(k);` followed by `x[k].<M rewritten>` (proofs `whose-value`, `whose-value-because`, `whose-value-other`, `whose-value-and`, `whose-value-read-only`, `whose-value-key-variable`, `whose-value-subject-call`).
- **REQ-13** (TAAK-171): IF `ContainKeys`/`ContainValues` gets a collection or becauseArgs, IF the type has no `Values` or no indexer on the key, OR IF the key of `ContainKey(..).WhoseValue` is a computed expression THEN THE SYSTEM SHALL give Manual with the reason (proofs `contain-keys-collection`, `contain-values-collection`, `whose-value-no-indexer`, `whose-value-computed-key`).
- **REQ-14** (TAAK-241): WHEN two links of the chain produce the same statement one after the other (`NotBeEmpty().And.OnlyContain(p)`: both start with `x.ShouldNotBeEmpty();`) THE SYSTEM SHALL write that statement once (proof `not-empty-only-contain`, combined proof `litedb-bson-assertions`).

## Assumptions

- `Compilation.ReplaceSyntaxTree` yields a usable semantic model for the split tree.
- Every `.And` in FA 7 returns the same assertions as `x.Should()`; otherwise Manual (type check).

- A `Dictionary` indexer after `ShouldContainKey` uses the same comparer as `ContainKey`; `ContainValue(s)` and `Contain` both compare with default equality (probe FA 7.2.2 vs Shouldly 4.3.0: arrays with equal contents and `"X"` versus `"x"` fail in both).

## Risks

- Property getters with side effects are executed twice (deliberately accepted: `IsSafeToEvaluateTwice` assumes getters are pure).
- A link pattern that returns several nodes that are not all statements is treated as unsupported (several statements, such as OnlyContain, are taken over).

- `ContainKey(k).WhoseValue.Should().Be(v)` becomes `x.ShouldContainKey(k); x[k].ShouldBe(v);` and not `ShouldContainKeyAndValue(k, v)`: this way the second-step rules (type conversion, `because`, strings) come from the existing pattern for the value, and every continuation (`StartWith`, `.And`) works the same. For a computed key there is no safe form: Manual.
- A nullable dictionary with `ContainValue(s)` gives `x!.Values...`: a null subject then fails with a NullReferenceException instead of an assertion failure; the test stays red.

## Models

Class diagram, sequence diagram and decision tree: [fa-and-chain-modellen.md](fa-and-chain-modellen.md).
