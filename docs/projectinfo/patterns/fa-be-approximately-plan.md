---
kingmadoc: 1
feature: "fa-be-approximately"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeApproximately/
---

# Feature: fa.be-approximately: BeApproximately and BeCloseTo become ShouldBe with tolerance

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-050.

## One-sentence summary

`x.Should().BeApproximately(y, tol)` and `t.Should().BeCloseTo(u, span)` become `x.ShouldBe(y, tol)`; `NotBeCloseTo` becomes `ShouldNotBe(u, span)`; `NotBeApproximately` becomes `System.Math.Abs(x - y).ShouldBeGreaterThan(tol)`.

## Scope (in / out)

**In scope**

- `(Not)BeApproximately` on double, float, decimal (also nullable subject).
- `(Not)BeCloseTo` on DateTime, DateTimeOffset, TimeSpan (also covers row `fa.be-close-to`).

**Out of scope**

- `BeCloseTo` on integers, TimeOnly and ExecutionTime (Manual: no Shouldly tolerance).
- Nullable expected value (Manual).
- `Not…` on a nullable subject (Manual).

## Requirements

- **REQ-1**: WHEN `BeApproximately(y, tol)` is on double/float/decimal THE SYSTEM SHALL give `x.ShouldBe(y, tol)` (proof `simple`).
- **REQ-2**: WHEN `because` without becauseArgs is present THE SYSTEM SHALL put it at the end as customMessage (proofs `because`, `close-to`).
- **REQ-3**: WHEN `NotBeApproximately(y, tol)` is present THE SYSTEM SHALL give `System.Math.Abs(x - y).ShouldBeGreaterThan(tol)`, with compound operands in parentheses (proof `not`).
- **REQ-4**: WHEN `(Not)BeCloseTo(u, span)` is on DateTime/DateTimeOffset/TimeSpan THE SYSTEM SHALL give `ShouldBe`/`ShouldNotBe(u, span)` (proof `close-to`).
- **REQ-5**: WHEN the subject is `Nullable<T>` THE SYSTEM SHALL give `x.ShouldNotBeNull().ShouldBe(..)` (proof `nullable`).
- **REQ-6**: IF an integer subject, `Not…` on nullable, becauseArgs or an AssertionScope THEN THE SYSTEM SHALL give Manual (proofs `int-close-to`, `nullable-not`, `because-args`, `assertion-scope`). becauseArgs: replaced by REQ-8.
- **REQ-7**: WHEN `BeApproximately` is a link after `.And` THE SYSTEM SHALL give a separate statement via `fa.and-chain` (proof `AndChain/equal-length-approximately`).
- **REQ-8** (Extension TAAK-097): WHEN `becauseArgs` with a constant, formattable `because` is present THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as customMessage (proof `because-args`); named arguments or a non-constant `because` stay Manual.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0): both pass at the boundary `|x - y| == tol` (double, decimal, DateTime); `Not…` fails at the boundary in FA and with `Math.Abs(..) > tol` / `ShouldNotBe(.., span)`.
- Shouldly 4.3.0 (reflection): `ShouldBe(double|float|decimal, …, tolerance, customMessage)`, `ShouldBe/ShouldNotBe(DateTime|DateTimeOffset|TimeSpan, …, TimeSpan, customMessage)`; no numeric `ShouldNotBe` with tolerance.

## Risks

- Infinity: FA passes on `+inf` vs `+inf`, Shouldly fails (inf - inf = NaN). NaN: FA throws ArgumentException, Shouldly fails; both red.

## Models

Class diagram, sequence diagram and decision tree: [fa-be-approximately-modellen.md](fa-be-approximately-modellen.md).
