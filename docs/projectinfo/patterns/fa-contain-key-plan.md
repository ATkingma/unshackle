---
kingmadoc: 1
feature: "fa-contain-key"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/
---
# Feature: fa.contain-key: dict.Should().ContainKey(k) becomes dict.ShouldContainKey(k)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-013.

## One-sentence summary

fa.contain-key: `ContainKey`/`NotContainKey` with one key becomes `ShouldContainKey`/`ShouldNotContainKey`; all other dictionary forms become Manual.

## Scope (in / out)

**In scope**

- Standalone `dict.Should().ContainKey(k)` and `NotContainKey(k)` (optionally with a `because` without `becauseArgs`) on `Dictionary`, `IDictionary` and `IReadOnlyDictionary`.
- `Manual(reason)` for `ContainKeys`/`NotContainKeys`, `ContainValue(s)`/`NotContainValue(s)`, `becauseArgs`, chains and a used result (`.WhoseValue`).

**Out of scope**

- `Contain(key, value)` (stays Manual in `fa.contain`), `HaveCount` on dictionaries.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `dict.Should().ContainKey(k)` is on a `Dictionary<,>` THE SYSTEM SHALL rewrite it to `dict.ShouldContainKey(k)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/dictionary.*.cs`).
- **REQ-2**: WHEN the subject is an `IDictionary<,>` THE SYSTEM SHALL give the same rewrite (proof `idictionary.*.cs`). For `IReadOnlyDictionary<,>` see REQ-11 (TAAK-168: `ShouldContainKey` does not exist there, CS0411).
- **REQ-3**: WHEN `NotContainKey(k)` is used THE SYSTEM SHALL give `ShouldNotContainKey(k)` (proof `not-contain-key.*.cs`).
- **REQ-4**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL take it over as `customMessage` (proof `because.*.cs`).
- **REQ-5**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `lambda.*.cs`).
- **REQ-6**: IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `because-args.*`).
- **REQ-7**: IF the result is used (`.And`, `.WhoseValue`) or the assertion is a continuation of another THEN THE SYSTEM SHALL give Manual (proofs `chain.*`, `which.*`, `and-chain.*`).
- **REQ-8**: IF `ContainKeys`/`NotContainKeys` is used THEN THE SYSTEM SHALL give Manual (proofs `keys.*`, `not-contain-keys.*`).
- **REQ-9**: IF `ContainValue(s)`/`NotContainValue` is used THEN THE SYSTEM SHALL give Manual, because Shouldly has no separate value assertion (proofs `contain-value.*`, `contain-values.*`, `not-contain-value.*`).
- **REQ-10**: IF the subject may be null in a nullable-enabled context THEN THE SYSTEM SHALL write `x.ShouldNotBeNull().ShouldContainKey(k)` (TAAK-021: FluentAssertions accepts a nullable subject, Shouldly's receiver does not; without this CS8602/CS8604 in projects with warnings-as-errors. `ShouldNotBeNull()` keeps FA's failure on null) (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/nullable.*`).
- **REQ-11** (TAAK-168): WHEN the subject is not an `IDictionary<,>` but has a public `bool ContainsKey(key)` (`IReadOnlyDictionary<,>`) THE SYSTEM SHALL give `x.ContainsKey(k).ShouldBeTrue()` (`NotContainKey`: `ShouldBeFalse()`), with `because` as `customMessage` (proofs `read-only.*`, `read-only-not-contain.*`). Chosen over `x.Keys.ShouldContain(k)`: `ContainsKey` keeps the dictionary's comparer and works for every implementation.
- **REQ-12** (TAAK-168): WHEN the subject has no `ContainsKey` but does have a public `bool Contains(key)` (`HttpHeaders`) THE SYSTEM SHALL give `x.Contains(k).ShouldBeTrue()`/`ShouldBeFalse()` (proof `headers.*`).
- **REQ-13** (TAAK-168): IF the subject has neither (e.g. `IEnumerable<KeyValuePair<,>>`) THEN THE SYSTEM SHALL give Manual (proof `enumerable-pairs.*`).
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` (proof `because-args.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-6 for this case.

## Assumptions

- Shouldly 4.3.0 has `ShouldContainKey`/`ShouldNotContainKey` only for `IDictionary<,>` (TAAK-168; earlier this wrongly also said `IReadOnlyDictionary`). Proven by the compile check of the after proofs.
- FluentAssertions also offers `ContainKey` on `IReadOnlyDictionary`, `HttpHeaders` and every `IEnumerable<KeyValuePair<,>>`, so the pattern checks the subject type (TAAK-168).

## Risks

- `HttpHeaders.Contains` ignores case, FA's `ContainKey` on headers does not: `ContainKey("x-a")` also passes for `X-A` after the migration (measured with a probe, TAAK-168). Intended behaviour for headers; `NotContainKey` becomes stricter through it, never weaker.
- The error message changes ("Additional Info:" instead of FA's because prefix): a documented difference.
- An own type with an FA `ContainKey` extension on another interface falls outside the proofs.

## Models

Class diagram, sequence and decision tree: [fa-contain-key-modellen.md](fa-contain-key-modellen.md).

## Open questions

- None.
