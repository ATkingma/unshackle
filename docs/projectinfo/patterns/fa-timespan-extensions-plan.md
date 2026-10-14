---
kingmadoc: 1
feature: "fa-timespan-extensions"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/TimeSpanExtensionsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Core/TimeSpanExtensionsPatternTests.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/TimeSpanExtensions/
---

# Feature: fa.timespan-extensions: `5.Seconds()` → `TimeSpan.FromSeconds(5)`

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-13 by KingmaDoc |

> Drawn up from TAAK-224 (FA corpus run round 4: 380 unmatched-use in Akka and Steeltoe, 23 in EventFlow for the month extensions).

## One-sentence summary

The time extensions from `FluentAssertions.Extensions` (`FluentTimeSpanExtensions`, and the months of `FluentDateTimeExtensions`) become the BCL call that FluentAssertions itself makes, so FA does not stay in the project just for this.

## Scope (in / out)

**In scope**

- `n.Ticks()`, `Milliseconds()`, `Seconds()`, `Minutes()`, `Hours()`, `Days()` on `int`/`long`/`double` → `TimeSpan.FromTicks/FromMilliseconds/FromSeconds/FromMinutes/FromHours/FromDays(n)`; also the static form `FluentTimeSpanExtensions.Seconds(5)`.
- `d.March(y)` (all twelve months) → `new DateTime(y, 3, d)` if day or year is constant.
- `FluentAssertions.Extensions` in the fix's `RemovedNamespaces`, so the using disappears when nothing needs it any more.

**Out of scope (Manual)**

- `Seconds(n, offset)`, `And(..)`, `Nanoseconds`/`Microseconds`, `At`, `AsUtc`/`AsLocal`, `Before`/`After`, `WithOffset`, `AddNanoseconds`/`AddMicroseconds`.
- A month with a computed day and a computed year (evaluation order reverses).

## Requirements

- **REQ-1**: WHEN `n.Seconds()` (or Ticks/Milliseconds/Minutes/Hours/Days) with one number is present THE SYSTEM SHALL produce `TimeSpan.FromSeconds(n)`, dropping parentheses around n (proof `simple`).
- **REQ-2**: WHEN the static form `FluentTimeSpanExtensions.Seconds(5)` is present THE SYSTEM SHALL produce the same (proof `static-call`).
- **REQ-3**: WHEN `TimeSpan`/`DateTime` does not bind at that location THE SYSTEM SHALL add `using System;` (proof `no-using-system`); WHEN an own type captures the name THE SYSTEM SHALL write `global::System.TimeSpan` (unit test `Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan`).
- **REQ-4**: WHEN `d.March(y)` with a constant day or year is present THE SYSTEM SHALL produce `new DateTime(y, 3, d)` (proof `month`).
- **REQ-5**: IF day and year are both computed THEN THE SYSTEM SHALL produce Manual (proof `month-computed`).
- **REQ-6**: IF another extension from `FluentAssertions.Extensions` is present (`And`, `Seconds(n, offset)`, …) THEN THE SYSTEM SHALL produce Manual (proofs `and`, `offset`). `At` on a `DateTime`: see REQ-8/9 (TAAK-243).
- **REQ-7**: WHEN an own `Seconds()` extension binds THE SYSTEM SHALL find nothing (unit test `Find_OwnSecondsExtension_FindsNothing`).
- **REQ-8** (TAAK-243): WHEN `d.At(h, m[, s[, ms]])` on a `DateTime` d that is safe to evaluate twice is present THE SYSTEM SHALL produce `new DateTime(d.Year, d.Month, d.Day, h, m, s, ms, d.Kind)` (missing parts 0), what FluentAssertions 7.2.2 computes (Kind included); WHEN d is a month call `n.October(y)` THE SYSTEM SHALL produce one constructor `new DateTime(y, 10, n, h, m, s)` and not rewrite the month call on its own (proofs `at-variable`, `month-at`; unit test `Rewrite_DateTimeAt_ConstructorWithDateParts`).
- **REQ-9** (TAAK-243): WHEN `d.At(time)` with a `TimeSpan` is present THE SYSTEM SHALL produce `d.Date + time` (with a month call: `new DateTime(y, m, n) + time`), in parentheses where it is an operand or receiver (proof `at-timespan`).
- **REQ-10** (TAAK-243): IF `At` gets microseconds/nanoseconds, OR its date is a computed expression THEN THE SYSTEM SHALL produce Manual (proofs `at-microseconds`, `at-computed`; unit test `Rewrite_DateTimeAtMicroseconds_Manual`).

## Assumptions

- FA 7.2.2 implements `Seconds(int)` as `TimeSpan.FromSeconds(seconds)` and `March(day, year)` as `new DateTime(year, 3, day)`. In .NET 9+ the new code picks the `long` overload of `TimeSpan.From*`; for integer values that is exactly equal to the `double` overload.

## Risks

- The Fixer converts the removed using in place to `using Shouldly;`, also in a file without asserts (harmless: the project gets Shouldly).

## Models

[fa-timespan-extensions-modellen.md](fa-timespan-extensions-modellen.md).

## Open questions

- None.
