---
kingmadoc: 1
feature: "epplus-dimension"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/DimensionPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/DimensionMapping.cs
    - src/Unshackle.Tests/Core/EPPlus/DimensionHarnessTests.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Tests/Proofs/EPPlus/Dimension/
---

# Feature: epplus.dimension: sheet.Dimension corners → LastRowUsed(XLCellsUsedOptions.All)/FirstColumnUsed(XLCellsUsedOptions.All) …, null kept

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from `claude/tasks/epplus-queue.md` row 7; council round 8, general rule, E1.

## One-sentence summary

epplus.dimension: `sheet.Dimension.End.Row` / `.End.Column` / `.Start.Row` / `.Start.Column` become `sheet.LastRowUsed(XLCellsUsedOptions.All).RowNumber()` / `LastColumnUsed(XLCellsUsedOptions.All).ColumnNumber()` / `FirstRowUsed(XLCellsUsedOptions.All).RowNumber()` / `FirstColumnUsed(XLCellsUsedOptions.All).ColumnNumber()`, keeping the empty-sheet null behaviour; any other use of `Dimension` is Manual.

## Scope (in / out)

**In scope (Fixed)**, `S` an EPPlus `ExcelWorksheet` (kept as it is):

| EPPlus | ClosedXML | Empty sheet |
| --- | --- | --- |
| `S.Dimension.End.Row` | `S.LastRowUsed(XLCellsUsedOptions.All).RowNumber()` | both throw `NullReferenceException` |
| `S.Dimension?.End.Row` | `S.LastRowUsed(XLCellsUsedOptions.All)?.RowNumber()` | both null |
| `.End.Column`, `.Start.Row`, `.Start.Column` | `LastColumnUsed(XLCellsUsedOptions.All).ColumnNumber()`, `FirstRowUsed(XLCellsUsedOptions.All).RowNumber()`, `FirstColumnUsed(XLCellsUsedOptions.All).ColumnNumber()` | same rule |

**Out of scope (Manual `UnmappedDimension`)**: `Dimension.Address`, `.Rows`, `.Columns`, a null check `S.Dimension == null`, passing `Dimension` on.
Cells with a style but no value count on both sides: EPPlus counts them in `Dimension`, and the rewrite passes `XLCellsUsedOptions.All` (council round 12, E16; harness cases with styled empty cells).

## Design

`DimensionPattern.Find` returns, for every `S.Dimension` on an EPPlus sheet, the whole chain up to `.Row`/`.Column` (member access or `?.` conditional access), else the member after `Dimension`, else `Dimension` itself; `DimensionMapping.Rewrite` maps the four corners. **Behaviour proof (E1)**: `DimensionHarnessTests` writes the expression's result (or -1 for null) to a second sheet, so `CellCompare` compares it; the scaffold (rows 2–5) is translated by hand. Each case also fails when the expression is wrong.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | The four corners write the same number as EPPlus (harness, 4 cases). |
| REQ-2 | `Dimension?.` keeps null on an empty sheet and the value on a filled sheet. |
| REQ-3 | Every case fails when the expression is wrong. |
| REQ-4 | Any other use of Dimension is Manual `UnmappedDimension`. |
| REQ-5 | A sheet with styled empty cells outside the values gives the same corners (`XLCellsUsedOptions.All`, E16); without `All` the harness differs. |

## See also

- [verify](epplus-dimension-verify.md), [modellen](epplus-dimension-modellen.md)
- `claude/tasks/epplus-queue.md` row 7, `claude/tasks/TAAK-309-epplus-to-closedxml.md`
