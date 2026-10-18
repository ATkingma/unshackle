# TAAK-281 — Removing a `using FluentAssertions;` inside `#if`/`#else` leaves a stray `#endif` (CS1028)

**Status**: DONE
**Branch**: worktree-agent-a703fd17d0ed2c1c1 (worktree .claude/worktrees/agent-a703fd17d0ed2c1c1)
**Root cause (found)**: `UsingDirectives.UpdateAsync` removed the unused using with `KeepNoTrivia`; Roslyn then drops the `#else` and the disabled text and leaves `#endif`. Two further causes in serilogmem: (a) a rewrite that replaces a node with `#if/#else` directives inside it (`.Should()` chain with `#if FLUENTASSERTIONS_8 ... #endif`) dropped the directives; (b) linked test files compiled by `...Unit.AwesomeAssertions9`, which references neither FluentAssertions nor Shouldly (CS0246).
**Fix**: (1) unused usings with directive/disabled-text leading trivia are removed with `KeepLeadingTrivia`; (2) `Fixer.Replace` returns Manual when the replaced node has directives inside that the new nodes do not have; (3) `Fixer.UnreachableFilesAsync`: a file compiled by a project that references the old library and by one that references neither old nor new library is not migrated (Manual, `encoding` row).
**Created**: 2026-10-18

## Context

Found in: serilogmem (`test/Serilog.Sinks.InMemory.Assertions.Tests.Unit/Repro.cs`: `#if AWESOMEASSERTIONS_9 using AwesomeAssertions; #else using FluentAssertions; #endif`). Build fails after `fix` (never a silent test change).

When the file has nothing left to rewrite, the using is removed together with its surrounding conditional directives (`#if`, the other branch, `#else`) but not the closing `#endif`: 48x CS1028 over the target frameworks. When the using is swapped to `Shouldly` inside the branch the result is fine.

## Steps

1. [x] Failing proof: `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/_Pending/using-in-conditional.before.cs` + `.after.cs` (parked in `_Pending`, which the proof runners skip; verified failing on `db05908` by copying it out of `_Pending`; the expected `after` text is provisional and may be adjusted).
2. [x] Fix: Remove only the using line (directives are trivia that belongs to other branches); or leave the using and report Manual when it sits in a conditional. Note: this repo is itself a FluentAssertions extension library (`InMemorySinkAssertions`), where a Shouldly migration is not meaningful; consider Manual for third-party `Should()` extension use.
3. [x] Move the proof out of `_Pending`, `dotnet test` green, KingmaDoc plan/verify updated for the touched pattern.
4. [x] Corpus: rerun the repo with `claude/tools/corpus-am.sh` (`after`) and check 0 regressions.

## See also

- `claude/tasks/CORPUSRUN-blind.md` (round 5)
- `src/Unshackle.Core/UsingDirectives.cs`
- [plan](../../../docs/projectinfo/features/using-removal-edge-cases-plan.md), [models](../../../docs/projectinfo/features/using-removal-edge-cases-modellen.md), [verify](../../../docs/projectinfo/features/using-removal-edge-cases-verify.md)
