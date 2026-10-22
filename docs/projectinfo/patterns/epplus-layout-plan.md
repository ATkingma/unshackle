---
kingmadoc: 1
feature: "epplus-layout"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/LayoutPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/LayoutMapping.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/LayoutShape.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Tests/Proofs/EPPlus/Layout/
    - src/Unshackle.Tests/Core/EPPlus/LayoutHarnessTests.cs
---

# Feature: epplus.layout: merge, freeze panes and row height → ClosedXML; widths Manual (E8)

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from `claude/tasks/epplus-queue.md` row 9; council round 8, general rule, E1 and E8.

## One-sentence summary

epplus.layout: `range.Merge = true;`, `sheet.View.FreezePanes(r, c);` with literal numbers and `sheet.Row(i).Height = h;` become their
ClosedXML statements, each proven by the harness; column widths and auto-fit are Manual `ColumnWidthDiffers` (E8).

## Scope (in / out)

**In scope (Fixed)** — the receiver (range, sheet, row) is kept as it is:

| EPPlus | ClosedXML | Why equal |
| --- | --- | --- |
| `R.Merge = true;` (R an `ExcelRange`) | `R.Merge();` | harness: same merged range |
| `S.View.FreezePanes(r, c);` (int literals ≥ 1) | `S.SheetView.Freeze(r - 1, c - 1);` | EPPlus names the first unfrozen cell, ClosedXML counts frozen rows/columns; harness: same split |
| `S.Row(i).Height = h;` | same text | both in points; harness: same height |

**Out of scope (Manual)**: `ColumnWidthDiffers` for `Column(i).Width = w` (E8: the same number gives a different column, pinned by
`ColumnWidth_SameNumber_DiffersInHarness`); `AutoFitDiffers` for `AutoFitColumns(..)` and `Column(i).AutoFit(..)`, for good (council
round 12, E13: the result depends on the installed fonts and each library's text measuring). `UnmappedEPPlusLayout` for `Merge = false` or a computed value, `FreezePanes` with non-literal arguments, and any other
row-height form.

## Design

`LayoutMapping.Shape` recognises the statement by the EPPlus type of its receiver (`ExcelRange(Base)`, `ExcelRow`, `ExcelColumn`,
`ExcelWorksheetView`); `LayoutPattern.Rewrite` maps it per `LayoutShape`. **Behaviour proof (E1, E14)**: full proofs in
`Proofs/EPPlus/Layout/_Combined/` (real `EPPlusFix` output), compared by the harness, whose snapshot has freeze and row-height facts;
`LayoutHarnessTests` changes each after (range, freeze count, height) and requires a difference.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | `Merge = true` writes the same merge; a wrong range is detected. |
| REQ-2 | `FreezePanes(2, 1)` and `(3, 2)` write the same split; an off-by-one is detected. |
| REQ-3 | `Row(i).Height = h` writes the same height; a different height is detected. |
| REQ-4 | Width and auto-fit are Manual `ColumnWidthDiffers` (proofs `column-width`, `auto-fit-columns`); the width difference is pinned. |
| REQ-5 | Unmerge and non-literal freeze are Manual `UnmappedEPPlusLayout` (proofs `unmerge`, `freeze-variable`). |

## See also

- [verify](epplus-layout-verify.md), [modellen](epplus-layout-modellen.md)
- `claude/tasks/epplus-queue.md` row 9, `claude/tasks/TAAK-309-epplus-to-closedxml.md`
