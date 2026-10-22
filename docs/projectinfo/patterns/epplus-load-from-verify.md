# Verification: epplus-load-from

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-load-from-plan.md`](epplus-load-from-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a516572e1f4f2f6d6` (EPPlus queue row 10).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `LoadFromHarnessTests.LoadFromCollection_RewrittenByPattern_WritesSameWorkbook` (8 cases) |
| REQ-2 | Met     | same test, wrong variant |
| REQ-3 | Met     | proof `LoadFrom/with-headers`; `UnprovenShape_IsManual` |
| REQ-4 | Met     | proofs `LoadFrom/datetime-property`, `LoadFrom/data-table`; `UnprovenShape_IsManual` |
| REQ-5 | Met     | `UnprovenShape_IsManual` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 2095 passed, 0 failed.
- No corpus run yet.

## Deviations

- Fixed shapes are proven with a hand-translated scaffold, like the other EPPlus rows.
- DateTime is not covered yet: no harness case was written for it, so it stays Manual (E4).
- `epplus.unsupported` still names the rewritten `InsertData` as an unresolved member of the EPPlus range until row 4 migrates the range (claim request).
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-load-from-plan.md), [modellen](epplus-load-from-modellen.md)
