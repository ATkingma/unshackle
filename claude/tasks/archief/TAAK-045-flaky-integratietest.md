# TAAK-045 — Flaky integration test `FixTwice_SampleShopCopy_MigratesProjectFilesAndBuildsAndTests`

**Status**: ✅ completed (2026-10-11)
**Created**: 2026-10-11
**Goal**: the integration test in `src/Unshackle.Tests/Integration/SampleShopTests.cs` also passes on the first `dotnet test` in a fresh (cold) worktree, without weakening the asserts.

## Report

Sometimes failed on the first `dotnet test` in a fresh worktree (no `obj/`/`bin/`), passed on repetition and on its own. Hint: a scan before `dotnet restore` gives 0 findings.

## Reproduction

- Cold copy (rsync without `bin/obj`) + `dotnet build` + `dotnet test`: 7 runs sequentially and 6 runs (3 parallel) — **not reproduced**, everything green. The original error message was therefore not captured.
- Found though (`/proc/<pid>/environ` of the test host): the outer `dotnet test` sets `MSBuildLoadMicrosoftTargetsReadOnly`, `MSBUILDFAILONDRIVEENUMERATINGWILDCARD`, `_MSBUILDTLENABLED` and `TESTINGPLATFORM_*` in the test host. The test's `dotnet restore/build/test` child processes inherited them, and used MSBuild node reuse and the MSBuild server that the cold build of the repo had just left behind.

## Cause (most likely)

The child `dotnet` processes were not isolated from the outer build/test: reused MSBuild nodes/server from the cold build and leaked MSBuild/test-platform environment variables. In a cold run (nodes just started, a busy system, possibly other agents building at the same time) the restore of the copy can then unpack differently/incompletely, after which MSBuildWorkspace loads without valid assets → 0 findings → the assert on the report rows fails (matches the hint). Races between the integration tests themselves are ruled out: they are in one class (= one xUnit collection, sequential) and each uses its own `TempDirectory` (GUID).

## Fix

`DotnetAsync` in `SampleShopTests.cs`: removes inherited `MSBuild*`, `_MSBuild*` and `TESTINGPLATFORM_*` variables and sets `MSBUILDDISABLENODEREUSE=1` and `DOTNET_CLI_USE_MSBUILD_SERVER=false`. Asserts unchanged; restore was already explicit in every test.

## Proof

- 5 consecutive cold runs (fresh copy without `bin/obj`, `dotnet build` + `dotnet test`): 5× 346/346 green.
- `dotnet build` 0 errors, `dotnet test` 346/346, `dotnet test --solution samples/SampleShop/SampleShop.slnx` 38/38, hook `check-pattern-docs.sh` exit 0.

## Remaining risk

Because the failure was not reproducible locally, the cause is not hard-proven. If it fails again: capture the full Shouldly message (contains stdout/stderr of `dotnet` or the report rows).

## See also

- `claude/docs/agent-werkwijze.md` §6 — main stays green
- `src/Unshackle.Tests/Integration/SampleShopTests.cs`
