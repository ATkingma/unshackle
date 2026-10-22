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
| REQ-1 | Met     | proof `Layout/_Combined/merge` (`StylesCombinedProofTests`, `EPPlusHarnessProofTests`); `LayoutProof_WrongLayout_IsSeenByHarness` |
| REQ-2 | Met     | proofs `Layout/_Combined/freeze-top-row`, `freeze-row-column`; same wrong-layout test |
| REQ-3 | Met     | proof `Layout/_Combined/row-height`; same wrong-layout test |
| REQ-4 | Met     | proofs `Layout/column-width`, `Layout/auto-fit-columns` (`AutoFitDiffers`, E13); `UnprovenShape_IsManualAndUnchanged`; `ColumnWidth_SameNumber_DiffersInHarness` |
| REQ-5 | Met     | proofs `Layout/unmerge`, `Layout/freeze-variable` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 2005 passed, 0 failed.
- No corpus run yet.

## Deviations

- Council round 12, E14: the Fixed shapes have full proofs in `Proofs/EPPlus/Layout/_Combined/` (real `EPPlusFix` output); the scaffold
  proofs are gone.
- `WorkbookSnapshot` now has freeze-pane and row-height facts (additive), so every harness proof compares them.
- `epplus.unsupported` skips the layout part of row-9 statements (`LayoutMapping.Receiver`), before or after the rewrite, and names only the sheet or range
  (`LayoutUnsupportedTests`).
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-layout-plan.md), [modellen](epplus-layout-modellen.md)
