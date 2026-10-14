---
kingmadoc: 1
feature: "fa-contain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/Contain/
---
# Feature: fa.contain: x.Should().Contain(item|predicate) becomes x.ShouldContain(...), NotContain becomes ShouldNotContain

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-007. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.contain: `Contain`/`NotContain` with one item, predicate or substring becomes `ShouldContain`/`ShouldNotContain`; strings explicitly get `Shouldly.Case.Sensitive`.

## Scope (in / out)

**In scope**

- `x.Should().Contain(item)`, `Contain(predicate)` and `NotContain(...)` (with an optional `because` without `becauseArgs`) as a standalone assertion on `IEnumerable<T>`, arrays, lists and `string` (substring).
- `Manual(reason)` for dictionaries, `Contain(IEnumerable)` (all items), occurrence constraints, `becauseArgs`, chains and a used result.

**Out of scope**

- `ContainKey` (row 11), `OnlyContain` (row 12), `ContainSingle`, `ContainInOrder`, `ContainAll/Any`.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `list.Should().Contain(item)` is on a collection THE SYSTEM SHALL rewrite it to `list.ShouldContain(item)` (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/list.*.cs`, `enumerable.*.cs` for `IEnumerable<T>` and array).
- **REQ-2**: WHEN `Contain(predicate)` or `NotContain(predicate)` with a lambda is used THE SYSTEM SHALL give `ShouldContain(predicate)` or `ShouldNotContain(predicate)` respectively (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/predicate.*.cs`).
- **REQ-3**: WHEN `NotContain(item)` is on a collection or string THE SYSTEM SHALL give `ShouldNotContain` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/not-contain.*.cs`).
- **REQ-4**: WHEN the subject is a `string` THE SYSTEM SHALL give `ShouldContain(x, Shouldly.Case.Sensitive)`, because Shouldly compares case-insensitively by default and FluentAssertions case-sensitively (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/string.*.cs`).
- **REQ-5**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL take it over as `customMessage`; for strings after `Case.Sensitive` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/because.*.cs`).
- **REQ-6**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/lambda.*.cs`).
- **REQ-7**: IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/because-args.*`).
- **REQ-8**: IF the result is used (`.And`/`.Which`, variable) THEN THE SYSTEM SHALL give Manual (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/chain.*`, `value-used.*`).
- **REQ-9**: IF the assertion itself is a continuation of another assertion THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/and-chain.*`).
- **REQ-10**: IF the subject is a dictionary THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/dictionary.*`).
- **REQ-11**: IF `Contain(IEnumerable)` (all items) or an occurrence constraint is used THEN THE SYSTEM SHALL give Manual (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/items.*`, `occurrence.*`).
- **REQ-12**: IF the subject may be null in a nullable-enabled context THEN THE SYSTEM SHALL write `x.ShouldNotBeNull().ShouldContain(...)` (TAAK-021: FluentAssertions accepts a nullable subject, Shouldly's receiver does not; without this CS8602/CS8604 in projects with warnings-as-errors. `ShouldNotBeNull()` keeps FA's failure on null) (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/nullable-string.*`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` (proof `.../Contain/because-args.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-7 for this case.

## Assumptions

- Shouldly 4.3.0 has `ShouldContain<T>(IEnumerable<T>, T, string? customMessage)` and `(…, Expression<Func<T,bool>>, …)`; proven by the compile check of the after proofs.
- The overload is determined via the declared parameter type of the FA symbol (type parameter, `string`, `Expression`, otherwise Manual).
- The `Fixer` manages `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- Without `Case.Sensitive` the rewrite would silently loosen (ignore case); therefore it is always added explicitly.
- The error message gets "Additional Info:" instead of FA's "because" prefix: a documented difference.
- Non-generic `IEnumerable` falls under Manual.

## Models

Class diagram, sequence diagram and decision tree: [fa-contain-modellen.md](fa-contain-modellen.md).

## Open questions

- None.
