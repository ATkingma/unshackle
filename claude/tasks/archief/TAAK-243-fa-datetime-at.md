# TAAK-243 — FA: `d.At(h, m)` / `n.October(y).At(h, m)` / `d.At(time)` from FluentAssertions.Extensions

**Status**: ✅ completed
**Finished**: 2026-10-14
**Created**: 2026-10-14 (FA corpus round 5 full: EventFlow, ~20 `At` calls in `Voyages.cs` and `RoutingServiceTests.cs`)
**Goal**: `At` on a `DateTime` no longer keeps the FluentAssertions package in the project.

## Context for the next agent

- FluentAssertions 7.2.2 `At(this DateTime date, int hours, int minutes, int seconds = 0, int milliseconds = 0, ...)` equals
  `new DateTime(date.Year, date.Month, date.Day, hours, minutes, seconds, milliseconds, date.Kind)`; `At(this DateTime, TimeSpan)`
  equals `date.Date + time`. Checked with a small program against the package: Unspecified/Utc/Local, sub-millisecond ticks,
  hour 25 throws `ArgumentOutOfRangeException` in both.
- The Fixer handles outer nodes first; a nested finding inside a replaced node becomes Manual ("Overlaps an earlier finding").
  So `At` takes its month-call date along and `Find` skips a month call that is the date of an `At`.

## Steps

- [x] 1. `TimeSpanExtensionsPattern.RewriteAt`: month date → `new DateTime(y, m, n, h, min, s)` (seconds padded, no 5-argument constructor);
  date safe to evaluate twice → constructor with `d.Year/Month/Day/Kind`; `TimeSpan` → `d.Date + t` (parenthesized as operand/receiver);
  microseconds/nanoseconds or computed date → Manual.
- [x] 2. Proofs `month-at`, `at-variable`, `at-timespan`, `at-computed`, `at-microseconds`; unit tests replaced (`Rewrite_DateTimeAt_*`).
- [x] 3. KingmaDoc plan REQ-8..10, verify, models (decision tree).

## Result

- 1148/1148 green.
