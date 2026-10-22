# Verification: epplus-layout

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-layout-plan.md`](epplus-layout-plan.md)         |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a516572e1f4f2f6d6` (EPPlus queue row 9).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `LayoutHarnessTests.LayoutShape_RewrittenByPattern_WritesSameWorkbook` (merge case) |
| REQ-2 | Met     | same test, `FreezePanes(2, 1)` and `(3, 2)` cases |
| REQ-3 | Met     | same test, row height case |
| REQ-4 | Met     | proofs `Layout/column-width`, `Layout/auto-fit-columns` (`AutoFitDiffers`, E13); `UnprovenShape_IsManualAndUnchanged`; `ColumnWidth_SameNumber_DiffersInHarness` |
| REQ-5 | Met     | proofs `Layout/unmerge`, `Layout/freeze-variable` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 2005 passed, 0 failed.
- No corpus run yet.

## Deviations

- Like epplus.styles, Fixed shapes are proven with a hand-translated scaffold, not `Proofs/EPPlus/Layout/*.after.cs` (the receiver is still
  EPPlus until rows 3–4).
- `WorkbookSnapshot` has no freeze-pane or row-height facts; `LayoutHarnessTests` adds them itself (harness files are read-only for
  this row). Adding them to `WorkbookSnapshot` would put them in every harness proof.
- `epplus.unsupported` skips the layout part of row-9 statements (`LayoutMapping.Receiver`), before or after the rewrite, and names only the sheet or range
  (`LayoutUnsupportedTests`).
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-layout-plan.md), [modellen](epplus-layout-modellen.md)
