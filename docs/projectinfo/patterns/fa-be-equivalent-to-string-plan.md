---
kingmadoc: 1
feature: "fa-be-equivalent-to-string"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/
---
# Feature: fa.be-equivalent-to-string: s.Should().BeEquivalentTo("x") becomes s.ShouldBe("x", StringCompareShould.IgnoreCase)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-009. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.be-equivalent-to-string: `BeEquivalentTo(string)` on a string becomes `ShouldBe(expected, StringCompareShould.IgnoreCase)`; `NotBeEquivalentTo` and chains are Manual.

## Scope (in / out)

**In scope**

- `s.Should().BeEquivalentTo("x")` (with an optional `because` without `becauseArgs`) as a standalone assertion on a `string`.
- `Manual(reason)` for `NotBeEquivalentTo`, named arguments or `becauseArgs` with a non-constant `because` (since TAAK-097), chains and a used result.

**Out of scope**

- `BeEquivalentTo` on objects and collections (row 14, `fa.be-equivalent-to-object`): Find only matches `StringAssertions`.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `s.Should().BeEquivalentTo("x")` is on a string THE SYSTEM SHALL rewrite it to `s.ShouldBe("x", StringCompareShould.IgnoreCase)`; both ignore only case (ordinal), whitespace and line endings stay significant (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/simple.*.cs`).
- **REQ-2**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL put it as `customMessage` between expected and `StringCompareShould.IgnoreCase`, because that is the Shouldly overload (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/because.*.cs`).
- **REQ-3**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/lambda.*.cs`).
- **REQ-4**: IF `NotBeEquivalentTo` is used THEN THE SYSTEM SHALL give Manual, because `ShouldNotBe` has no case-insensitive string overload (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/not-equivalent.*`).
- **REQ-5** (replaced by REQ-8): IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/because-args.*`).
- **REQ-6**: IF the result is used (`.And`/`.Which`, variable) THEN THE SYSTEM SHALL give Manual (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/chain.*`, `value-used.*`).
- **REQ-7**: IF the assertion itself is a continuation of another assertion THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/and-chain.*`).
- **REQ-8** (Extension TAAK-097): WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args.*.cs`); named arguments or a non-constant `because` stay Manual.

## Assumptions

- FA 7.2.2 `StringAssertions.BeEquivalentTo(string, because, becauseArgs)` is ordinal case-insensitive; confirmed with a test program (ABC/abc, é/É and ǅ/ǆ equal; leading/trailing whitespace, \r\n versus \n, İ/i and ß/SS different) and identical to Shouldly `IgnoreCase`.
- FA 7.2.2 has no `options =>` overload for strings.
- Shouldly has `ShouldBe(string, string, string customMessage, StringCompareShould)`; proven by the compile check of the after proofs.
- A `null` subject or `null` expectation behaves the same (both pass only for null/null).
- The `Fixer` manages `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- The error message gets "Additional Info:" instead of FA's "because" prefix: a documented difference.
- A non-string subject falls outside Find and stays untouched.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-equivalent-to-string-modellen.md](fa-be-equivalent-to-string-modellen.md).

## Open questions

- None.
