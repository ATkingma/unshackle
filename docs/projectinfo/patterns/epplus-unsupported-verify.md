# Verification: epplus-unsupported

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-unsupported-plan.md`](epplus-unsupported-plan.md)   |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                     |

> Verified by an AI agent against the code of branch `worktree-agent-adf8e0efedecc7b94` (TAAK-309, queue row 11).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `Unsupported/cells` (full reason text pinned) |
| REQ-2 | Met     | proof `Unsupported/chart` |
| REQ-3 | Met     | proof `Unsupported/public-api` |
| REQ-4 | Met     | proof `Unsupported/internal-api`; `EPPlusFixTests.UnsupportedPattern_PublicApiInTestProject_NotE6` |
| REQ-5 | Met     | `EPPlusFixTests.UnsupportedPattern_UnresolvedEPPlus5Member_NamedWithItsReceiver` |
| REQ-6 | Met     | `EPPlusFixTests.Fix_SaveAsStream_ManualAndSolutionUntouched`, `Fix_TwoPackagesInOneBlock_Manual` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: see the TAAK-309 progress log (row 11 entry).

## Deviations

- Reason codes are typed (`EPPlusReason`, council round 10 C1); the report prints `Code: reason`.
- "Packable" is approximated as in MediatR's `AssemblyResolver` (a library without a test framework); a library with `IsPackable=false` still
  counts as packable, so it errs on the side of Manual.
- In `Fix_TwoPackagesInOneBlock_Manual` the other uses in the block get an extra Unmigrated row next to the two package rows (the package
  pattern reports the creations there, not the scope).

## See also

- [plan](epplus-unsupported-plan.md), [modellen](epplus-unsupported-modellen.md)
