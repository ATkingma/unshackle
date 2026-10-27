---
kingmadoc: 1
feature: "epplus-address"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/AddressPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/DimensionMapping.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusReason.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Tests/Proofs/EPPlus/Address/
---

# Feature: epplus.address: range Rows/Columns and Start/End → ClosedXML range address

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-27 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from council round 13, E18 step 2 (`claude/tasks/COUNCIL-round-13.md`), TAAK-327.

## One-sentence summary

epplus.address: `ws.Cells[..].Rows` / `.Columns` / `.Start.Row` / `.End.Column` (and the other two corners) on a range taken straight from a worksheet become `.RowCount()` / `.ColumnCount()` / `.RangeAddress.FirstAddress.RowNumber` / `.RangeAddress.LastAddress.ColumnNumber`, and the `Cells[..]` receiver is left to `epplus.package` (`ws.Range(..)`).

## Scope (in / out)

**In scope (Fixed)**: the receiver is `ws.Cells[r1, c1, r2, c2]` (four int expressions) or `ws.Cells["A1:C2"]` (a literal range address whose second corner is not before the first), with `ws` an EPPlus `ExcelWorksheet`; the member is `ExcelAddressBase.Rows`, `.Columns`, or `.Start`/`.End` followed by `.Row`/`.Column`.

**Out of scope (Manual `UnmappedAddress`)**:

| Shape | Reason |
| --- | --- |
| single cell (`ws.Cells["B2"].Rows`) | ClosedXML's `Cell(..)` is an `IXLCell` without `RowCount()` |
| reversed literal address (`ws.Cells["C5:A2"]`) | harness: EPPlus throws on it, ClosedXML sorts it |
| non-literal string address | its order cannot be checked |
| bare `.Start` / `.End` (an `ExcelCellAddress` value) | no one-to-one type |

**Range locals (TAAK-337, council round 16 E25)**: the same reads on an `ExcelRange` local (`var r = ws.Cells[..];` or `ExcelRange r = ...`) are Fixed when the local is the one variable of a declaration statement in a block, initialized from an in-order `ws.Cells[..]` range as above, and never written again (no assignment, `ref`/`out` argument or `++`/`--`). `WorkbookUses` then makes the initializer `ws.Range(..)` (an explicit type becomes `IXLRange`), and every use of the local must be such an address read or a by-value argument to a range helper `epplus.flow-types` migrates; else Manual. Any other range local is Manual `UnmappedAddress`. A range parameter (`private static int Size(ExcelRange r)`) is mapped by `epplus.flow-types`; a field stays `epplus.unsupported`.

**Limitation (council round 16 E26)**: int coordinates are accepted as any int expression, as for the existing `Cells[..]` → `Range(..)` mapping. A non-positive or out-of-sheet coordinate throws in both libraries, only with a different exception type; a valid program behaves the same.

## Design

`AddressPattern` runs after `epplus.dimension` and before `epplus.package`, like `DimensionPattern`: it rewrites only the tail and keeps `ws.Cells[..]`. `WorkbookUses.MapCellUse` accepts a range whose parent is the written tail (`IsAddressRead`) and makes it `ws.Range(..)`; `IsCovered` accepts the still-unbound int tail as a cell value. The new names are added to the rewritten names `epplus.unsupported` skips.

**Behaviour proof (E1)**: `Address/_Combined/rows-columns` runs the whole fix and through `EPPlusHarnessProofTests` writes the same workbook, including a reversed int range `Cells[5, 4, 2, 3]` (both libraries sort the corners: rows 2-5, columns 3-4).

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | `.Rows` / `.Columns` of an int or literal range become `.RowCount()` / `.ColumnCount()`. |
| REQ-2 | `.Start`/`.End` `.Row`/`.Column` become `.RangeAddress.FirstAddress`/`LastAddress` `.RowNumber`/`.ColumnNumber`. |
| REQ-3 | With the full fix the result writes the same workbook (harness), reversed int range included. |
| REQ-4 | A single cell or reversed literal address is Manual `UnmappedAddress`. |
| REQ-8 | (R3, council round 19) An `UnmappedAddress` read in a member `epplus.unsupported` reports anyway gets no row of its own: its reason is folded into that one row ("Also epplus.address UnmappedAddress: ..."), as E31; the summary counts the node once. |
| REQ-5 | A bare `.Start`/`.End` is Manual `UnmappedAddress`. |
| REQ-6 | (E25) An address read on a range local declared once from a mapped `Cells[..]` is Fixed and the local becomes a ClosedXML range; the harness writes the same workbook. |
| REQ-7 | (E25) A range local that is written again is Manual `UnmappedAddress`. |

## See also

- [verify](epplus-address-verify.md), [modellen](epplus-address-modellen.md)
- [epplus-dimension plan](epplus-dimension-plan.md)
- `claude/tasks/TAAK-327-epplus-flow-types.md`, `claude/tasks/epplus-queue.md`
