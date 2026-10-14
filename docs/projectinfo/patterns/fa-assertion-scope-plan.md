---
kingmadoc: 1
feature: "fa-assertion-scope"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/AssertionScope/
---
# Feature: fa.assertion-scope: report AssertionScope as Manual, never silently rewrite assertions inside it

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-031.

## One-sentence summary

fa.assertion-scope: `new AssertionScope(...)` becomes `Manual` with the advice `ShouldSatisfyAllConditions` (since TAAK-100: a plain scope with only assertions becomes `ShouldSatisfyAllConditions`, see REQ-7), and every FA assertion inside such a scope becomes `Manual` instead of `Fixed`, because Shouldly stops at the first failure.

## Scope (in / out)

**In scope**

- Shared check `FluentAssertionsSyntax.IsInsideAssertionScope`: body of `using (new AssertionScope(...))` and statements after `using var scope = new AssertionScope();` in the same block or deeper; also nested scopes and target-typed `new()`.
- All FA patterns ask for the check as the first step in `Rewrite`.
- Pattern `fa.assertion-scope`: every `new AssertionScope(...)` (also with a context string) → `Manual`.

**Out of scope**

- ~~Automatically rewriting to `ShouldSatisfyAllConditions`~~: done since TAAK-100 for a plain scope with only FA assertion statements; scopes with other statements, `Discard()` or `AddReportable` stay Manual.
- Subclasses: `AssertionScope` is `sealed` in FA 7.2.2 (CS0509), so not possible.

## Requirements

- **REQ-1** (replaced by REQ-7/REQ-8): WHEN `using (new AssertionScope())` is used THE SYSTEM SHALL report the scope as Manual (proof `AssertionScope/using-block`).
- **REQ-2** (replaced by REQ-7/REQ-8): WHEN `using var scope = new AssertionScope()` or `using AssertionScope scope = new()` is used THE SYSTEM SHALL report the scope as Manual (proofs `AssertionScope/using-var`, `AssertionScope/target-typed`).
- **REQ-3**: WHEN the scope gets a context string THE SYSTEM SHALL report the scope as Manual (proof `AssertionScope/context`).
- **REQ-4**: IF an FA assertion is in the body of a scope `using` (also nested deeper) THEN THE SYSTEM SHALL give Manual with "Inside an AssertionScope" (proofs `ShouldBe/assertion-scope-using-block`, `ShouldBe/assertion-scope-nested`; unit test `IsInsideAssertionScope_DependsOnEnclosingScope`).
- **REQ-5**: IF an FA assertion follows a `using var` with a scope in the same block or deeper THEN THE SYSTEM SHALL give Manual (proof `ShouldBe/assertion-scope-using-var`; unit test).
- **REQ-6**: WHEN an assertion is before the `using var`, after the scope block, or in another `using` THE SYSTEM SHALL let the pattern rewrite it as usual (unit test `IsInsideAssertionScope_DependsOnEnclosingScope`, `false` cases).
- **REQ-7** (Extension TAAK-100): WHEN `using (new AssertionScope()) { ... }` or a single `using var s = new AssertionScope();` (variable otherwise unused) without arguments contains only FA assertion statements, without `await` and ref locals, THE SYSTEM SHALL replace the scope with `x.ShouldSatisfyAllConditions(() => { a; }, () => { b; });` (`x` = first subject that is safe to evaluate twice), after which the assertion patterns rewrite the lambdas (proofs `_Combined/scope-using-block`, `_Combined/scope-using-var`). Probe: both collect all failures.
- **REQ-8** (Extension TAAK-100): IF the scope has a context string, the scope variable is used (`Discard()`), it contains another statement or an `await` THEN THE SYSTEM SHALL give Manual with the advice `ShouldSatisfyAllConditions`, and the assertions inside stay Manual "Inside an AssertionScope" (proofs `AssertionScope/context`, `discard`, `other-statement`, `await`).

## Assumptions

- The type is recognized via the semantic model (`FluentAssertions.Execution.AssertionScope`), not by name.
- A lambda inside the scope that runs later is syntactically inside the scope: Manual is then too cautious, but safe.

## Risks

- More Manual reports in projects that use many scopes; that is deliberate (no silent behaviour change).

## Models

Class diagram, sequence and decision tree: [fa-assertion-scope-modellen.md](fa-assertion-scope-modellen.md).

## Open questions

- None.
