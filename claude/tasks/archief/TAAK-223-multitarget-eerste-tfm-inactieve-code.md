# TAAK-223 — Multi-target project: only the first TFM is migrated, code under `#if !NETFRAMEWORK` stays behind

**Status**: ✅ done (2026-10-13)
**Created**: 2026-10-13 (FA corpus run round 4)
**Goal**: migrate files in multi-target projects in a TFM in which the code is active.

## Context for the next agent

- LiteDB `7b71bc4`: 709 `unmatched-use` Manuals (593× `x.Should()`), mainly in `LiteDB.Tests/Engine/Coordinator*_Tests.cs` and `SharedCoordination*_Tests.cs`. Besides that 14 `usings` findings "Code in an inactive #if branch".
- `LiteDB.Tests.csproj` has `TargetFrameworks net462;net481;net8.0;net10.0`, with `NETFRAMEWORK` defined for net4x. The files concerned are entirely inside `#if !NETFRAMEWORK`.
- `Fixer.FixAsync` (`src/Unshackle.Core/Fixer.cs`, "A multi-target project is loaded once per target framework") groups documents per path and takes `file.First()`. Here that is the net462 copy, in which the code is inactive. `ProjectPackages.RemainingUsesAsync` does find the calls in another TFM, and that gives `unmatched-use`.

## Steps

- [x] 1. Per file choose the project copy in which the branches with FA calls are active (e.g. the copy with the most active FA calls), or fix per TFM and only write if the outcome is equal.
- [x] 2. Unit test with a multi-target project and `#if !NETFRAMEWORK`.
- [x] 3. LiteDB repeat: unmatched-use ≈ 0, build and tests green.

## Outcome

- `Fixer.PassCopiesAsync`: per file one pass per copy with a differing set of inactive `#if` code (a file without directives: only the first copy). Pass 2+ works on the text so far and only fixes nodes that were inactive in all earlier passes (`Fixer.NewCode`, reparse with the earlier `ParseOptions`); shared code is therefore fixed and reported once. Conflict checks (TAAK-174/198/199) per pass (`FixCopyAsync`). The usings row "inactive #if branch" of an earlier pass is dropped when a later pass updates the usings.
- Tests: `FixerTests.FixAsync_CodeOnlyActiveInLaterTargetFramework_IsFixedOnceInEveryBranch` (compiles in both TFMs), `FixAsync_ManualInSharedCode_IsReportedOnceAcrossTargetFrameworks`. No proof: this is Fixer behaviour, not a pattern.
- LiteDB (`<corpus>/corpus-fa/_results/r5-litedb/`): unmatched-use 709 → 2, usings 15 → 2, Fixed 6917 → 7649; build green; only the 26 known regressions of TAAK-222.

## See also

- `claude/tasks/CORPUSRUN-fa.md` (round 4)
- `claude/tasks/patroon-queue.md`
