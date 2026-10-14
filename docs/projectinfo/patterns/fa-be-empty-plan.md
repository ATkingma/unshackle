---
kingmadoc: 1
feature: "fa-be-empty"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/
---
# Feature: fa.be-empty: x.Should().BeEmpty()/NotBeEmpty() becomes x.ShouldBeEmpty()/ShouldNotBeEmpty()

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-006. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.be-empty: x.Should().BeEmpty()/NotBeEmpty() becomes x.ShouldBeEmpty()/ShouldNotBeEmpty(), for strings, collections and dictionaries.

## Scope (in / out)

**In scope**

- `x.Should().BeEmpty()` / `NotBeEmpty()` (with an optional `because` without `becauseArgs`) as a standalone assertion, on string, `IEnumerable<T>`, arrays, lists and dictionaries.
- `Manual(reason)` for named arguments or `becauseArgs` with a non-constant `because` (since TAAK-097), chains (`.And`) and used result.

**Out of scope**

- `BeNullOrEmpty` / `NotBeNullOrEmpty`: own pattern (Shouldly: `ShouldBeNullOrEmpty`).
- Splitting `NotBeEmpty().And...` chains.
- Managing `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `list.Should().BeEmpty()` is on a collection THE SYSTEM SHALL rewrite it to `list.ShouldBeEmpty()` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/be-empty-list.*.cs`).
- **REQ-2**: WHEN `list.Should().NotBeEmpty()` is on a collection THE SYSTEM SHALL rewrite it to `list.ShouldNotBeEmpty()` (proof `.../BeEmpty/not-be-empty-list.*.cs`).
- **REQ-3**: WHEN the subject is a `string` THE SYSTEM SHALL use the same `ShouldBeEmpty`/`ShouldNotBeEmpty` (proof `.../BeEmpty/string.*.cs`).
- **REQ-4**: WHEN the subject is a dictionary THE SYSTEM SHALL rewrite it (proof `.../BeEmpty/dictionary.*.cs`); likewise `IEnumerable<T>` and array (proof `.../BeEmpty/enumerable.*.cs`).
- **REQ-5**: WHEN a `because` without `becauseArgs` is present THE SYSTEM SHALL take it over as `customMessage` (proof `.../BeEmpty/because.*.cs`).
- **REQ-6**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `.../BeEmpty/lambda.*.cs`).
- **REQ-7** (replaced by REQ-10): IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `.../BeEmpty/because-args.*`).
- **REQ-8**: IF the assertion has a continuation (`.And`) or the result is used as a value THEN THE SYSTEM SHALL give Manual (proofs `.../BeEmpty/chain.*`, `value-used.*`).
- **REQ-9**: IF the assertion itself is a continuation of another assertion (`x.Should().NotBeNull().And.NotBeEmpty()`) THEN THE SYSTEM SHALL give Manual (proof `.../BeEmpty/and-chain.*`; in the full Fix `fa.and-chain` splits such a chain).
- **REQ-10** (Extension TAAK-097): WHEN `becauseArgs` with a constant, formattable `because` is present THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `.../BeEmpty/because-args.*.cs`); named arguments or a non-constant `because` stay Manual.

## Assumptions

- Shouldly 4.3.0 has `ShouldBeEmpty`/`ShouldNotBeEmpty` for `string` and `IEnumerable<T>`; dictionaries fall under `IEnumerable<KeyValuePair<,>>` (proven by the compile check of `dictionary.after.cs`).
- Non-generic `IEnumerable` is not proven and is outside the proofs.
- The `Fixer` manages `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- The failure message gets "Additional Info:" instead of FA's "because" prefix: documented difference.
- `ShouldBeEmpty` on `null` fails, just like FA's `BeEmpty`.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-empty-modellen.md](fa-be-empty-modellen.md).

## Open questions

- None.
