# TAAK-323 — Missing SDK in global.json: clean load failure instead of a crash

**Status**: done
**Created**: 2026-10-26
**Branch**: `pool/sdk-crash-wip`
**Goal**: `unshackle scan` on a solution whose `global.json` pins an SDK that is not installed reports "solution could not be loaded" with the missing version, instead of crashing.

## Context for the next agent

- The MSBuildWorkspace build host fails in `hostfxr_resolve_sdk2` and Roslyn rethrows that as its internal `Microsoft.CodeAnalysis.MSBuild.RemoteInvocationException` from `OpenSolutionAsync`/`OpenProjectAsync`; it is not a workspace failure, so the existing handler never saw it.
- `MsBuildSolutionStore.LoadAsync` now catches it (matched by type name, plus `hostfxr_resolve_sdk2` in the message) and throws `SolutionLoadException` with one reason naming the requested version and the `global.json` path, parsed from hostfxr's message.

## Steps

- [x] 1. Reproduce with a test (temp folder next to the test binaries, `global.json` pinning 1.0.0 with `rollForward: disable`)
- [x] 2. Map the exception to `SolutionLoadException`
- [x] 3. Build + full test run green

## Progress log

- 2026-10-26: reproduced, fixed, all 3727 tests green.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`, `src/Unshackle.Tests/DataAccess/MsBuildSolutionStoreTests.cs` |
| **How**   | Catch filter + reason parsing |
| **Why**   | A user error (wrong SDK) must give an actionable message, not a stack trace |

## See also

- `claude/docs/agent-werkwijze.md`
