# TAAK-284 — Find the test that fails only under load

**Status**: step 3/3 — fix proven, final verify-head run
**Branch**: worktree-agent-a3891483d818cf3a5 (worktree `.claude/worktrees/agent-a3891483d818cf3a5`)
**Created**: 2026-10-18
**Goal**: name and fix (or isolate) the test that failed once in `dotnet test` on 2026-10-18 (twice: 1 of 1558, 1 of 1571) while corpus builds ran in parallel, and passed in every rerun.

## Context for the next agent

- Three full runs on an idle machine: 1571/1571 green each time (`<corpus>/flaky/run1..3`). CI green on every push.
- Both failures happened while agents ran corpus builds (high CPU/RAM). Suspects: integration tests that build/test a copy of `samples/SampleShop` (`src/Unshackle.Tests/Integration/`), timeouts, or shared temp folders.
- **Likely cause found (2026-10-18):** two `verify-head.sh` runs (an agent and the main session) shared the folder `<scratch>/verify-head`; the second run deleted the first one's worktree while its tests ran. The script now uses a unique `mktemp` folder per run. If no failure occurs again, close this task.
- `claude/tools/verify-head.sh` now keeps an xUnit TRX report and prints the failing test names on failure.

- **Reproduced (2026-10-18)**: `dotnet test --no-build -- --max-threads 4` with 32 busy loops added: `CombinedProofTests.CombinedAutoMapper_Before_BecomesAfter(partial-mocked-class-under-test)` failed (`ReportService.Line` kept `_mapper.Map` instead of `ToCustomerDto()`). Baseline 1 failure in the first 3 runs (`<corpus>/flaky284/run*.log`).
- **Root cause**: `TestSolution`, `TestProjects` and `VendoredPackagesTests` each used one static `AdhocWorkspace`, so every test solution had the same `SolutionId`. `MapOwners` caches its plan per `SolutionId` (`s_cache`), so parallel AutoMapper tests overwrote each other's cache entry. A test whose entry was replaced mid-Fixer-run rebuilt its plan from the already-rewritten solution and got a different set of kept maps. Timing dependent, hence only under load. Production is unaffected (a real solution has a unique id).
- **Proof**: before the fix 2 of 4 runs failed under load; after it 6 of 6 runs green under the same load (`<corpus>/flaky284/after/`).
- **Fix**: `TestSolution.NewEmpty()` (a fresh `AdhocWorkspace` per solution) used by all three; regression test `TestSolutionTests`.

## Steps

- [x] 1. On the next failure, take the test name from `verify-head.sh` output (or its `verify-head-results/*.trx`)
- [x] 2. Reproduce under load (e.g. run the test while a corpus build runs); find the cause
- [x] 3. Fix (timeout, isolation, unique temp dir) with proof, or mark it with a trait and document why

## See also

- `claude/docs/agent-werkwijze.md` §6, `claude/tools/verify-head.sh`
