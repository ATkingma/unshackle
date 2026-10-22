# Verification: epplus-dimension

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-dimension-plan.md`](epplus-dimension-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a516572e1f4f2f6d6` (EPPlus queue row 7).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `DimensionHarnessTests.DimensionShape_RewrittenByPattern_WritesSameWorkbook` (End/Start × Row/Column) |
| REQ-2 | Met     | same test, the three `?.` cases |
| REQ-3 | Met     | same test, wrong variant |
| REQ-4 | Met     | proofs `Dimension/address`, `Dimension/null-check`; `OtherDimensionUse_IsManual` (3 cases) |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 2080 passed, 0 failed.
- No corpus run yet.

## Deviations

- Fixed shapes are proven with a hand-translated scaffold, like epplus.styles and epplus.layout.
- `epplus.unsupported` skips the rewritten `LastRowUsed` etc. and names only the sheet (`DimensionUnsupportedTests`).
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-dimension-plan.md), [modellen](epplus-dimension-modellen.md)
