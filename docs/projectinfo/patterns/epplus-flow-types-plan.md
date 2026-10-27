---
kingmadoc: 1
feature: "epplus-flow-types"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusReason.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Tests/Proofs/EPPlus/FlowTypes/
---

# Feature: epplus.flow-types: private ExcelWorksheet parameter → IXLWorksheet

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-27 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from council round 13, E18 step 1 (`claude/tasks/COUNCIL-round-13.md`), TAAK-327. Extended by council round 16, E20-E24
> (`claude/tasks/COUNCIL-round-16.md`), TAAK-336: helper chains, the first Fixed field and return shapes, one row per node.
> E20: private members only; internal and public members stay `FlowMemberNotPrivate`.

## One-sentence summary

epplus.flow-types: a private helper method with one `ExcelWorksheet` parameter takes `IXLWorksheet` instead, when every use of the parameter in its body is one the package rewrite already proves and every caller passes a migrated worksheet; with that, helper chains (E23), a worksheet field written from a migrated package (E21) and a helper returning `package.Workbook.Worksheets.Add("literal")` (E22).

## Scope (in / out)

**In scope (Fixed)**: `private` ordinary method (not virtual/override/extern/partial) of a non-partial type, with a body, exactly one EPPlus-typed parameter, which is a by-value `ExcelWorksheet` (no `ref`/`out`/`params`/default), no EPPlus return type; only invoked (never a method group); parameter uses are `Cells[..]` as a covered `Value =` target, a migrated style/merge/InsertData receiver, a migrated layout/dimension use, or a `Name` read (`WorkbookUses`, the same rules as a worksheet local); every caller's argument is no longer an EPPlus worksheet. `PackagePattern` accepts a worksheet local passed by position to such a method.

**In scope since TAAK-336 (Fixed)**:

- **E23 chains**: a helper passes its worksheet parameter (by position) to another private helper of the same tree. All helpers linked that way (callers and callees) flip together; each must pass the rules above, the links must not form a cycle (recursion) and the longest chain is at most 3 methods (the `TypeArgumentClosure` bound, A11).
- **E21 field**: a `private` field of type `ExcelWorksheet` (one variable, no initializer) of a non-partial type, referenced by its bare name only. Every write is a statement `_f = p.Workbook.Worksheets.Add("x")` (or a lookup `PackagePattern` proves) of an `ExcelPackage` local `p`, directly in that local's scope; any other use is a `Name` read, or comes later in the scope of a write (`PackagePattern` maps it as a worksheet local). `PackagePattern` rewrites the write and those uses; this pattern rewrites the declaration once every write's package is migrated.
- **E22 return**: a private helper whose one EPPlus parameter is a by-value `ExcelPackage` and whose body only returns `package.Workbook.Worksheets.Add("valid literal")` (the parameter used nowhere else) becomes `IXLWorksheet Helper(XLWorkbook package) => package.Worksheets.Add("literal")`, when every caller passes a migrated package. `PackagePattern` accepts the package local passed to it, with the result dropped or standing where a worksheet may (`.Name`, a worksheet local).
- **E25 range parameter (TAAK-337)**: a private helper whose one EPPlus parameter is a by-value `ExcelRange`, used only for its address (`.Rows`, `.Columns`, `.Start`/`.End` `.Row`/`.Column`, rewritten here as `epplus.address` does), takes `IXLRange`, when every caller passes a migrated range: `ws.Cells[..]` (in order, made `ws.Range(..)` by `WorkbookUses`) or an E25 range local. Passing the range on to another helper is not mapped (no range chains).
- **E24**: `epplus.unsupported` skips nodes `epplus.flow-types` or `epplus.address` still find after they ran (they left them Manual), so each node has one row with the most specific reason. Exception: a flow-types node whose signature is E6 public API of a packable project keeps its `PublicApiExposesOldLibrary` row.

**Out of scope (Manual)**:

| Shape | Reason |
| --- | --- |
| public/internal/protected or virtual method, partial type | `FlowMemberNotPrivate` (E18: private only; callers may be out of reach) |
| another EPPlus parameter, ref/out/params/optional worksheet | `FlowUseNotMigrated` |
| an EPPlus return type other than the E22 shape (a non-literal or invalid sheet name, a lookup, `Action<ExcelWorksheet>`) | `FlowReturnNotMapped` (E22: no constant propagation through calls) |
| method group (`Action<ExcelWorksheet> a = Fill`) | `FlowUseNotMigrated` |
| a parameter use the package rules do not map (a cell read, a rename, passing it to a non-chain method) | that use's code (`CellReadDiffers`, `UncoveredRuntimeType`, `FlowUseNotMigrated`) |
| a caller passes a worksheet no pattern migrated (field, other helper) | `FlowCallerNotMigrated` |
| a recursive helper chain, or one longer than 3 methods | `FlowUseNotMigrated` (E23) |
| a range parameter used other than for its address, or passed on | `FlowUseNotMigrated` (E25) |
| a field other than the E21 shape (constructor parameter source, initializer, qualified access, use outside a write's scope) | `FlowFieldNotMapped` (E21) |

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
| REQ-7 | A field fed by a constructor parameter (or any other non-E21 field) is Manual `FlowFieldNotMapped`. |
| REQ-8 | A returned sheet with a non-literal name (or any other non-E22 EPPlus return) is Manual `FlowReturnNotMapped`. |
| REQ-9 | E21: a private worksheet field written from a migrated package local becomes `IXLWorksheet`; the full fix writes the same workbook. |
| REQ-10 | E22: a private helper returning `package.Workbook.Worksheets.Add("literal")` takes `XLWorkbook` and returns `IXLWorksheet`; the full fix writes the same workbook. |
| REQ-11 | E23: a chain of up to 3 private helpers flips together; recursion or a longer chain is Manual `FlowUseNotMigrated`. |
| REQ-12 | E24: a node flow-types or address left Manual has one row (no extra `epplus.unsupported` row). |
| REQ-13 | E25: a private helper with one by-value ExcelRange parameter used for its address takes IXLRange; with the full fix, `Cells[..]` and range-local arguments migrate and the result writes the same workbook. |
| REQ-14 | E25: any other use of the range parameter is Manual `FlowUseNotMigrated`. |

## See also

- [verify](epplus-flow-types-verify.md), [modellen](epplus-flow-types-modellen.md)
- `claude/tasks/TAAK-327-epplus-flow-types.md`, `claude/tasks/archief/TAAK-336-epplus-flow-followups.md`, `claude/tasks/epplus-queue.md` row 13
