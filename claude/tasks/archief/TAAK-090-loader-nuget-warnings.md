# TAAK-090 — loader: NuGet warnings and missing projects not fatal

**Status**: completed
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md`, action 0

## Problem

5 of 10 corpus solutions did not load. Polly, EventFlow, SmartEnum: NuGet audit (NU1902/NU1903). Bogus: NU1701.
Specification: a project is missing from the tarball. For users worse than any Manual.

## Analysis

- Reproduction (scratchpad, only restore + load): Polly failed with "Msbuild failed when processing the file
  '…Polly.Benchmarks.csproj' with message: Package … has a known high severity vulnerability". Polly has **no**
  TreatWarningsAsErrors: Roslyn's `MSBuildWorkspace` reports every MSBuild message as `WorkspaceDiagnosticKind.Failure`,
  also a warning. Our handler turned every Failure into a `SolutionLoadException`.
- `NuGetAudit=false` alone does not help: the warnings are already in `project.assets.json` and are logged again in the design-time
  build. With TreatWarningsAsErrors they become errors that make `ResolvePackageAssets` fail, hence also
  `WarningsNotAsErrors`.

## What, where, how

- **Where**: `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`, `ISolutionStore.Warnings` (Core),
  `ScanCommand` (shows warnings).
- **How**: global properties `NuGetAudit=false`, `WarningsNotAsErrors=NU1701;NU1900..NU1905` in `MSBuildWorkspace.Create`.
  Failures are classified after loading: if the message names a project file that was loaded, or one that does not exist →
  warning; otherwise fatal. The CS0246/CS0234 check stays unchanged and is the safety net (it gets the
  warnings in its error message).
- **Why a missing project is safe**: nothing on disk to rewrite; dependent projects still fail on the
  CS0246 check. An existing but non-loaded project stays fatal (the fix would skip its code but still change shared
  package files).
- **Alternative rejected**: recognising the NU code in the text (the message contains no code and is localised).

## Result

| Solution      | Before                    | After                                                                           |
| ------------- | ------------------------- | ------------------------------------------------------------------------------- |
| Polly         | fails (NU1903)            | loads, 4285 Fixed / 804 Manual                                                  |
| EventFlow     | fails (NU1902/3 as error) | loads, 459 / 65 (restore itself requires `-p:NuGetAudit=false`)                 |
| SmartEnum     | fails                     | loads, 299 / 68                                                                 |
| Bogus         | fails (NU1701)            | loads, 796 / 154 (after restoring `Builder`, which the solution restore skipped) |
| Specification | fails                     | rightly fails: core project outside the tarball → CS0246 in dependent projects  |

Tests: `SampleShopTests.Load_VulnerablePackageAndMissingProject_LoadsRestWithWarnings` (integration),
`CommandTests.ScanExecuteAsync_LoadWarnings_ShowsThemAndReturnsOk`. `Scan_NotRestored_FailsWithRestoreAdvice` stays green.

## See also

- `docs/projectinfo/architectuur/core.md` — section TAAK-090
- `claude/tasks/ANALYSE-fa-100.md`
