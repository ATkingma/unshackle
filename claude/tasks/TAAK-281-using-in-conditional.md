# TAAK-281 — Removing a `using FluentAssertions;` inside `#if`/`#else` leaves a stray `#endif` (CS1028)

**Status**: open (regression found in blind round 5, TAAK-276; not fixed there)
**Created**: 2026-10-18

## Context

Found in: serilogmem (`test/Serilog.Sinks.InMemory.Assertions.Tests.Unit/Repro.cs`: `#if AWESOMEASSERTIONS_9 using AwesomeAssertions; #else using FluentAssertions; #endif`). Build fails after `fix` (never a silent test change).

When the file has nothing left to rewrite, the using is removed together with its surrounding conditional directives (`#if`, the other branch, `#else`) but not the closing `#endif`: 48x CS1028 over the target frameworks. When the using is swapped to `Shouldly` inside the branch the result is fine.

## Steps

1. [x] Failing proof: `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/_Pending/using-in-conditional.before.cs` + `.after.cs` (parked in `_Pending`, which the proof runners skip; verified failing on `db05908` by copying it out of `_Pending`; the expected `after` text is provisional and may be adjusted).
2. [ ] Fix: Remove only the using line (directives are trivia that belongs to other branches); or leave the using and report Manual when it sits in a conditional. Note: this repo is itself a FluentAssertions extension library (`InMemorySinkAssertions`), where a Shouldly migration is not meaningful; consider Manual for third-party `Should()` extension use.
3. [ ] Move the proof out of `_Pending`, `dotnet test` green, KingmaDoc plan/verify updated for the touched pattern.
4. [ ] Corpus: rerun the repo with `claude/tools/corpus-am.sh` (`after`) and check 0 regressions.

## See also

- `claude/tasks/CORPUSRUN-blind.md` (round 5)
- `src/Unshackle.Core/UsingDirectives.cs`
