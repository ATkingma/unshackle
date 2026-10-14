---
kingmadoc: 1
feature: "fa-be-greater-less"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-15, REQ-16, REQ-17]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/
---
# Feature: fa.be-greater-less: BeGreaterThan/BeLessThan (+ .And chains) become ShouldBeGreaterThan/ShouldBeLessThan, chains split

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-010. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.be-greater-less: comparisons (`BeGreaterThan`, `BeLessThan`, `...OrEqualTo`, `BeInRange`, `BePositive`, `BeNegative`) become `ShouldBe...`; `.And` chains are split into separate statements.

## Scope (in / out)

**In scope**

- `BeGreaterThan`, `BeGreaterThanOrEqualTo` (+ alias `BeGreaterOrEqualTo`), `BeLessThan`, `BeLessThanOrEqualTo` (+ alias `BeLessOrEqualTo`), `BeInRange`, `NotBeInRange`, `BePositive`, `BeNegative`, on non-nullable numeric types and other `IComparable<T>` types, with an optional `because`.
- `.And` chains: since REVIEW-A split by `fa.and-chain`, which hands every link to this pattern.
- `Manual(reason)` for nullable subjects, `becauseArgs`, a used result, chains with other assertions, subjects with calls and chains outside a block.

**Out of scope**

- `BeCloseTo`, `BeApproximately` (`fa.be-approximately`). `BeAfter`/`BeBefore`/… are in scope since TAAK-095.
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeGreaterThan(n)`, `BeGreaterThanOrEqualTo`/`BeGreaterOrEqualTo`, `BeLessThan` or `BeLessThanOrEqualTo`/`BeLessOrEqualTo` is a standalone assertion on a non-nullable numeric type THE SYSTEM SHALL give `x.ShouldBeGreaterThan(n)` or respectively `ShouldBeGreaterThanOrEqualTo`, `ShouldBeLessThan`, `ShouldBeLessThanOrEqualTo` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/simple.*.cs`).
- **REQ-2**: WHEN the subject is a non-numeric type that implements `IComparable<T>` of itself (TimeSpan, Version) THE SYSTEM SHALL give the same rewrite (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/comparable.*.cs`).
- **REQ-3**: WHEN `BeInRange(min, max)` or `NotBeInRange(min, max)` is used THE SYSTEM SHALL give `ShouldBeInRange` or `ShouldNotBeInRange` respectively (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/in-range.*.cs`).
- **REQ-4**: WHEN `BePositive()` or `BeNegative()` is used on int, long, short, decimal, double or float THE SYSTEM SHALL give `ShouldBePositive()` or `ShouldBeNegative()` respectively (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/sign.*.cs`).
- **REQ-5**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL take it over as the last argument (`customMessage`) (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/because.*.cs`).
- **REQ-6**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/lambda.*.cs`).
- **REQ-7**: WHEN an `.And` chain consists only of comparisons, is a statement in a block and the subject is a name or member access on names THE SYSTEM SHALL split the chain into one statement per link, each with its own `because` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/and-chain.*.cs`).
- **REQ-8**: IF the subject is a nullable (`int?`) or unknown type THEN THE SYSTEM SHALL give Manual, because `ShouldBeGreaterThan<T>` requires `IComparable<T>` of a non-nullable T (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/nullable.*`).
- **REQ-9**: IF `BePositive`/`BeNegative` is used on a type other than int, long, short, decimal, double or float THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/sign-byte.*`).
- **REQ-10** (replaced by REQ-17): IF `becauseArgs` are used THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/because-args.*`).
- **REQ-11**: IF the result is used (variable or lambda with a value, or a chain in a lambda) THEN THE SYSTEM SHALL give Manual (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/value-used.*`, `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-used.*`, `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-lambda.*`).
- **REQ-12** (dropped, REVIEW-A): IF an `.And` chain contains another assertion THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-other.*`).
- **REQ-13** (replaced: `fa.and-chain` hoists the subject, TAAK-098): IF the subject of a chain is a call or another expression with possible side effects THEN THE SYSTEM SHALL give Manual, because splitting evaluates it twice (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-subject-call.*`).
- **REQ-14**: IF a split chain is not directly in a block (e.g. `if` without braces) THEN THE SYSTEM SHALL give Manual (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-no-block.*`).
- **Extension TAAK-095**: WHEN `BeAfter`/`BeOnOrAfter`/`BeBefore`/`BeOnOrBefore` is used THE SYSTEM SHALL give `ShouldBeGreaterThan`/`…OrEqualTo`/`ShouldBeLessThan`/`…OrEqualTo` (proofs `dates`, `dates-nullable`, `AndChain/dates`). Probe: equal, ±1 tick, other Kind, same instant with another offset, DateOnly: same verdict.
- **REQ-15** (Extension REVIEW-A): WHEN the value has a different type than the subject (`byte`/`short`) THE SYSTEM SHALL cast the value to the subject type (`ConvertExpected`), otherwise Manual (proof `byte-short`).
- **REQ-16**: IF `BeNegative()` is used on double/float THEN THE SYSTEM SHALL give `(x < 0).ShouldBeTrue([because])` (subject in parentheses if it is not a simple expression), because FA fails on NaN while `ShouldBeNegative`/`ShouldBeLessThan(0)` pass (CompareTo orders NaN below everything); `x < 0` is false for NaN and -0.0, just as in FA (proofs `negative-double`, `negative-float`). `BePositive` is the same in both libraries and stays `ShouldBePositive()`. See TAAK-166.
- **Extension TAAK-169**: WHEN the value only reaches the subject type via an own implicit conversion (`BsonValue b; b.Should().BeGreaterThan(0)`) THE SYSTEM SHALL add a cast to the subject type (`b.ShouldBeGreaterThan((BsonValue)0)`), via `ConvertExpected` (proof `implicit-conversion.*`).
- **REQ-17** (Extension TAAK-097): WHEN `becauseArgs` accompany a constant, formattable `because` THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args`); named arguments or a non-constant `because` stay Manual.

## Assumptions

- Shouldly 4.3.0 has `ShouldBeGreaterThan<T>(T, string? customMessage)` (likewise for `LessThan`, `...OrEqualTo`, `InRange`, `NotInRange` with `where T : IComparable<T>`) and `ShouldBePositive`/`ShouldBeNegative` for int, long, short, decimal, double, float; proven by the compile check of the after proofs.
- Property getters in a chain subject (`order.Total`) have no side effects (shortcut, documented in code).
- The `Fixer` sets the trivia and indentation of the extra statements and manages the usings.

## Risks

- A property with side effects as the subject of a chain is read twice; limited by only allowing names and member accesses.
- Error messages differ from FluentAssertions (Shouldly puts "Additional Info:" before the `because`).
- Probed (TAAK-166, FA 7.2.2 vs Shouldly 4.3.0, double and float): NaN, -0.0, 0.0, ±Epsilon, ±Infinity, ±1 give identical results for `BePositive`; for `BeNegative` only NaN differs. int/long/short/decimal are equal. The error message of `ShouldBeTrue` is less informative (no value).

## Models

Class, sequence and decision model of this pattern: [fa-be-greater-less-modellen.md](fa-be-greater-less-modellen.md).

## Open questions

- None.
