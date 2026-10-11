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

# Feature: fa.be-approximately: BeApproximately en BeCloseTo worden ShouldBe met tolerance

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-050.

## One-sentence summary

`x.Should().BeApproximately(y, tol)` en `t.Should().BeCloseTo(u, span)` worden `x.ShouldBe(y, tol)`; `NotBeCloseTo` wordt `ShouldNotBe(u, span)`; `NotBeApproximately` wordt `System.Math.Abs(x - y).ShouldBeGreaterThan(tol)`.

## Scope (in / out)

**In scope**

- `(Not)BeApproximately` op double, float, decimal (ook nullable subject).
- `(Not)BeCloseTo` op DateTime, DateTimeOffset, TimeSpan (dekt ook rij `fa.be-close-to`).

**Out of scope**

- `BeCloseTo` op integers, TimeOnly en ExecutionTime (Manual: geen Shouldly-tolerance).
- Nullable verwachte waarde (Manual).
- `Not…` op een nullable subject (Manual).

## Requirements

- **REQ-1**: WHEN `BeApproximately(y, tol)` op double/float/decimal staat THE SYSTEM SHALL `x.ShouldBe(y, tol)` geven (proof `simple`).
- **REQ-2**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage achteraan zetten (proofs `because`, `close-to`).
- **REQ-3**: WHEN `NotBeApproximately(y, tol)` staat THE SYSTEM SHALL `System.Math.Abs(x - y).ShouldBeGreaterThan(tol)` geven, samengestelde operanden tussen haakjes (proof `not`).
- **REQ-4**: WHEN `(Not)BeCloseTo(u, span)` op DateTime/DateTimeOffset/TimeSpan staat THE SYSTEM SHALL `ShouldBe`/`ShouldNotBe(u, span)` geven (proof `close-to`).
- **REQ-5**: WHEN het subject `Nullable<T>` is THE SYSTEM SHALL `x.ShouldNotBeNull().ShouldBe(..)` geven (proof `nullable`).
- **REQ-6**: IF een integer-subject, `Not…` op nullable, becauseArgs of een AssertionScope THEN THE SYSTEM SHALL Manual geven (proofs `int-close-to`, `nullable-not`, `because-args`, `assertion-scope`). becauseArgs: vervangen door REQ-8.
- **REQ-7**: WHEN `BeApproximately` een schakel na `.And` is THE SYSTEM SHALL via `fa.and-chain` een los statement geven (proof `AndChain/equal-length-approximately`).
- **REQ-8** (Uitbreiding TAAK-097): WHEN `becauseArgs` met een constante, formatteerbare `because` staat THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (proof `because-args`); named arguments of een niet-constante `because` blijven Manual.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0): beide slagen op de grens `|x - y| == tol` (double, decimal, DateTime); `Not…` faalt op de grens bij FA en bij `Math.Abs(..) > tol` / `ShouldNotBe(.., span)`.
- Shouldly 4.3.0 (reflectie): `ShouldBe(double|float|decimal, …, tolerance, customMessage)`, `ShouldBe/ShouldNotBe(DateTime|DateTimeOffset|TimeSpan, …, TimeSpan, customMessage)`; geen numerieke `ShouldNotBe` met tolerance.

## Risks

- Oneindig: FA slaagt bij `+inf` vs `+inf`, Shouldly faalt (inf - inf = NaN). NaN: FA gooit ArgumentException, Shouldly faalt; beide rood.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-approximately-modellen.md](fa-be-approximately-modellen.md).
