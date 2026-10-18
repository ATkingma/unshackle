# TAAK-284 — Find the test that fails only under load

**Status**: step 1/3 — waiting for the next occurrence
**Created**: 2026-10-18
**Goal**: name and fix (or isolate) the test that failed once in `dotnet test` on 2026-10-18 (twice: 1 of 1558, 1 of 1571) while corpus builds ran in parallel, and passed in every rerun.

## Context for the next agent

- Three full runs on an idle machine: 1571/1571 green each time (`<corpus>/flaky/run1..3`). CI green on every push.
- Both failures happened while agents ran corpus builds (high CPU/RAM). Suspects: integration tests that build/test a copy of `samples/SampleShop` (`src/Unshackle.Tests/Integration/`), timeouts, or shared temp folders.
- `claude/tools/verify-head.sh` now keeps an xUnit TRX report and prints the failing test names on failure.

## Steps

- [ ] 1. On the next failure, take the test name from `verify-head.sh` output (or its `verify-head-results/*.trx`)
- [ ] 2. Reproduce under load (e.g. run the test while a corpus build runs); find the cause
- [ ] 3. Fix (timeout, isolation, unique temp dir) with proof, or mark it with a trait and document why

## See also

- `claude/docs/agent-werkwijze.md` §6, `claude/tools/verify-head.sh`
