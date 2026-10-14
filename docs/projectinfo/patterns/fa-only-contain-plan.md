---
kingmadoc: 1
feature: "fa-only-contain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/OnlyContain/
---
# Feature: fa.only-contain: x.Should().OnlyContain(pred) becomes x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-015.

## One-sentence summary

fa.only-contain: `x.Should().OnlyContain(pred)` becomes two statements, `x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);`, because FluentAssertions fails on an empty collection and Shouldly does not.

## Scope (in / out)

**In scope**

- `OnlyContain(predicate)` (optionally with a `because` without `becauseArgs`) on a statement in a block, with a simple subject (name, `this`, member path), on any generic `IEnumerable<T>` (list, array, dictionary).
- `Manual(reason)` for a used result, chains, `becauseArgs`, a subject with a call, and an assertion outside a block (lambda, `if` without braces).

**Out of scope**

- `AllSatisfy` (own pattern `fa.all-satisfy`), `OnlyHaveUniqueItems`.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().OnlyContain(pred)` is a statement in a block THE SYSTEM SHALL replace it with `x.ShouldNotBeEmpty();` and `x.ShouldAllBe(pred);` (proof `OnlyContain/simple.*.cs`).
- **REQ-2**: WHEN the subject is an `IEnumerable<T>`, array or dictionary THE SYSTEM SHALL give the same rewrite (proofs `enumerable.*`, `array.*`, `dictionary.*`).
- **REQ-3**: WHEN the subject is a member path (`other.Items`, `this.Items`) THE SYSTEM SHALL rewrite it (proof `member-subject.*`).
- **REQ-4**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL put it as `customMessage` on both statements (proof `because.*`).
- **REQ-5**: IF `becauseArgs` or named arguments are used THEN THE SYSTEM SHALL give Manual (proof `because-args.*`).
- **REQ-6**: IF the result is used (`.And`, variable) THEN THE SYSTEM SHALL give Manual (proofs `chain.*`, `used.*`).
- **REQ-7**: IF the assertion comes after `.And` THEN THE SYSTEM SHALL give Manual (proof `and-chain.*`).
- **REQ-8**: IF the subject is a call THEN THE SYSTEM SHALL give Manual, because splitting evaluates the subject twice (proof `subject-call.*`).
- **REQ-9**: IF the assertion is not a statement directly in a block (lambda body, `if` without braces) THEN THE SYSTEM SHALL give Manual, because two statements cannot go there (proofs `lambda.*`, `no-block.*`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL put `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` on both statements (proof `because-args.*`, now `.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-5.
- **Extension TAAK-098**: WHEN the subject cannot safely be evaluated twice (e.g. a call) THE SYSTEM SHALL hoist it to `var subject = x;` and then give `subject.ShouldNotBeEmpty(); subject.ShouldAllBe(pred);` (proof `subject-call.*`, now `.after.cs`). Replaces REQ-8.
- **Extension TAAK-100**: IF the assertion is inside an `AssertionScope` that is not converted THEN THE SYSTEM SHALL give Manual "Inside an AssertionScope" (shared check in `FluentAssertionPattern.Rewrite`).

## Assumptions

- Behaviour established with a test program (FA 7.2.2, Shouldly 4.3.0): `OnlyContain` fails on an empty collection ("but the collection is empty"), `ShouldAllBe` passes; hence the extra `ShouldNotBeEmpty()`.
- In FA the predicate is always an `Expression<Func<T,bool>>`, just like in Shouldly's `ShouldAllBe`; no delegate variant, no conversion needed.
- Getters in the subject are pure (evaluating twice is safe).

## Risks

- A `null` collection gives an assertion failure in FA and an `ArgumentNullException`/assertion failure of `ShouldNotBeEmpty` in Shouldly: a different exception, same failure.
- The error message changes ("Additional Info:" instead of FA's because prefix).

## Models

Class diagram, sequence and decision tree: [fa-only-contain-modellen.md](fa-only-contain-modellen.md).

## Open questions

- None.
