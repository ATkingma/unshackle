---
kingmadoc: 1
feature: "fa-be-null"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/
---
# Feature: fa.be-null: x.Should().BeNull()/NotBeNull() becomes x.ShouldBeNull()/ShouldNotBeNull()

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-10T23:42+02:00 by KingmaDoc 0.3.0.dev9 |

> Drawn up from TAAK-004. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.be-null: x.Should().BeNull()/NotBeNull() becomes x.ShouldBeNull()/ShouldNotBeNull(), for reference types and `Nullable<T>`.

## Scope (in / out)

**In scope**

- `x.Should().BeNull()` / `NotBeNull()` (with an optional `because` without `becauseArgs`) as a standalone assertion, on reference types (string, object, collections) and `Nullable<T>`.
- `Manual(reason)` for `becauseArgs`, chains (`.And`, `.Which`) and a used result.

**Out of scope**

- Converting `NotBeNull().Which...` to `var v = x.ShouldNotBeNull()` (Shouldly returns the value, but that is splitting chains).
- `BeNull`/`NotBeNull` on `Action`/`Func` subjects: FA has no `BeNull` there.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeNull()` is on a reference type THE SYSTEM SHALL rewrite it to `x.ShouldBeNull()` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/be-null.*.cs`).
- **REQ-2**: WHEN `x.Should().NotBeNull()` is on a reference type THE SYSTEM SHALL rewrite it to `x.ShouldNotBeNull()` (proof `.../BeNull/not-be-null.*.cs`).
- **REQ-3**: WHEN `BeNull`/`NotBeNull` has a `because` without `becauseArgs` THE SYSTEM SHALL take it over as `customMessage` (proof `.../BeNull/because.*.cs`).
- **REQ-4**: WHEN the subject is a `Nullable<T>` (`int?`, `bool?`) THE SYSTEM SHALL use the same `ShouldBeNull`/`ShouldNotBeNull` (proof `.../BeNull/nullable-value.*.cs`).
- **REQ-5**: WHEN the subject is a collection or array THE SYSTEM SHALL rewrite it (proof `.../BeNull/collection.*.cs`); WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `.../BeNull/lambda.*.cs`).
- **REQ-6**: IF `becauseArgs` or named arguments are used THEN THE SYSTEM SHALL give Manual (proof `.../BeNull/because-args.*`).
- **REQ-7**: IF a continuation follows the assertion (`.And`, `.Which`) or the result is used as a value THEN THE SYSTEM SHALL give Manual (proofs `.../BeNull/chain.*`, `value-used.*`).
- **REQ-8**: IF the assertion itself is a continuation of another assertion (`x.Should().NotBeEmpty().And.NotBeNull()`) THEN THE SYSTEM SHALL give Manual (proof `.../BeNull/and-chain.*`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` (proof `.../BeNull/because-args.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-6 for this case.
- **Extension TAAK-100**: IF the assertion is inside an `AssertionScope` that `fa.assertion-scope` does not convert THEN THE SYSTEM SHALL give Manual ("Inside an AssertionScope").

## Assumptions

- Shouldly 4.3.0 has `ShouldBeNull<T>(this T? actual, string? customMessage)` and `ShouldNotBeNull<T>(...)`, which compile for reference types and `Nullable<T>` (proven by the compile check of `nullable-value.after.cs`).
- `ShouldNotBeNull()` returns the value; with a discarded result that does not matter.
- The `Fixer` manages `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- The error message gets "Additional Info:" instead of FA's "because" prefix: a documented difference.
- After `x.Should().NotBeNull(); x!.Y` the `!` stays: harmless, Shouldly's `[NotNull]` only makes it superfluous.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-null-modellen.md](fa-be-null-modellen.md).

## Open questions

- None. A `NotBeNull().Which` rewrite can later become its own pattern.
