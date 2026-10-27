---
kingmadoc: 1
feature: "epplus-flow-types"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusReason.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Tests/Proofs/EPPlus/FlowTypes/
---

# Feature: epplus.flow-types: private ExcelWorksheet parameter → IXLWorksheet

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-27 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from council round 13, E18 step 1 (`claude/tasks/COUNCIL-round-13.md`), TAAK-327.

## One-sentence summary

epplus.flow-types: a private helper method with one `ExcelWorksheet` parameter takes `IXLWorksheet` instead, when every use of the parameter in its body is one the package rewrite already proves and every caller passes a migrated worksheet.

## Scope (in / out)

**In scope (Fixed)**: `private` ordinary method (not virtual/override/extern/partial) of a non-partial type, with a body, exactly one EPPlus-typed parameter, which is a by-value `ExcelWorksheet` (no `ref`/`out`/`params`/default), no EPPlus return type; only invoked (never a method group); parameter uses are `Cells[..]` as a covered `Value =` target, a migrated style/merge/InsertData receiver, a migrated layout/dimension use, or a `Name` read (`WorkbookUses`, the same rules as a worksheet local); every caller's argument is no longer an EPPlus worksheet. `PackagePattern` accepts a worksheet local passed by position to such a method.

**Out of scope (Manual)**:

| Shape | Reason |
| --- | --- |
| public/internal/protected or virtual method, partial type | `FlowMemberNotPrivate` (E18: private only; callers may be out of reach) |
| another EPPlus parameter, EPPlus return, ref/out/params/optional worksheet | `FlowUseNotMigrated` |
| method group (`Action<ExcelWorksheet> a = Fill`) | `FlowUseNotMigrated` |
| a parameter use the package rules do not map (a cell read, a rename, passing it on) | that use's code (`CellReadDiffers`, `UncoveredRuntimeType`, `FlowUseNotMigrated`) |
| a caller passes a worksheet no pattern migrated (field, other helper) | `FlowCallerNotMigrated` |
| fields and return types of `ExcelPackage`/`ExcelWorksheet`/`ExcelRange` | not found by this pattern; still `epplus.unsupported` (council question) |

## Design

`FlowTypesPattern` runs after `epplus.package` and before `epplus.unsupported`. `Analyze` checks the method shape, that every reference is an invocation, and maps the body with `new WorkbookUses(model, method, parameter)` (no package: a rename and further flow are refused). `Migratable` exposes the same check to `WorkbookUses.IsFlowArgument`, so the package rewrite of the caller and this rewrite of the callee are decided by one rule. On rewrite the callers are checked once more on the updated tree: an argument still typed `ExcelWorksheet` blocks. The parameter type becomes `IXLWorksheet` and the body uses get the replacements `WorkbookUses` collected.

**Behaviour proof (E1)**: the body is mapped by the same rules as a worksheet local, already harness-proven; `FlowTypes/_Combined/parameter` runs the whole fix and through `EPPlusHarnessProofTests` writes the same workbook.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | A private helper with one ExcelWorksheet parameter and mapped uses takes IXLWorksheet, body rewritten. |
| REQ-2 | With the full fix, a package local passed to that helper is Fixed too, and the result writes the same workbook (harness). |
| REQ-3 | A non-private helper is Manual `FlowMemberNotPrivate`. |
| REQ-4 | An unmapped use of the parameter is Manual with that use's reason. |
| REQ-5 | A caller passing an unmigrated worksheet is Manual `FlowCallerNotMigrated`. |
| REQ-6 | A method group of the helper is Manual. |

## See also

- [verify](epplus-flow-types-verify.md), [modellen](epplus-flow-types-modellen.md)
- `claude/tasks/TAAK-327-epplus-flow-types.md`, `claude/tasks/epplus-queue.md` row 13
