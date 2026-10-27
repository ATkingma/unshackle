# TAAK-327 — EPPlus flow types (`epplus.flow-types`, council E18)

**Status**: step 1 parameters Fixed, fields/returns Manual (pending council); step 2 (`epplus.address`) done; corpus re-measure open
**Created**: 2026-10-27
**Goal**: stop helper methods that take an `ExcelWorksheet` from holding every package in their file on EPPlus, as council round 13 E18 decides.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-13.md` E18. Order: (1) `ExcelPackage`/`ExcelWorksheet`/`ExcelRange` as field, parameter or return type, rewritten when every use in the flow is Fixed, private/internal only; (2) `ExcelAddressBase.Rows/Columns` and `Start/End` with harness proofs; (3) tables; (4) drawings (likely Manual codes only).
- Why: CORPUSRUN-epplus round 1 counts `ExcelWorksheet` as a type 222 times in `UnmigratedEPPlusApi`; helpers taking a worksheet block the method that calls them, and so the package rewrite (E17 per project).
- Narrowest provable slice built (scope of E18 (1) is not fully clear, see Council questions): a **private** method with **one by-value `ExcelWorksheet` parameter**. Reasoning:
  - private in a non-partial type: every caller is in the same syntax tree, so one semantic model sees the whole flow (no SymbolFinder, no cross-project E17 risk);
  - the body is mapped by `WorkbookUses`, the same rules as a worksheet local, which are already harness-proven (E1); without a package in reach, a rename (`Name = "x"`) and passing the worksheet on are refused, because the added sheet names are unknown;
  - `PackagePattern` (via `WorkbookUses.IsFlowArgument`) and `FlowTypesPattern` share one check (`FlowTypesPattern.Migratable`), so caller and callee flip together; the callee re-checks callers on the updated tree, so an unmigrated caller (a field, another helper) is Manual `FlowCallerNotMigrated`.
- New reason codes: `FlowMemberNotPrivate`, `FlowUseNotMigrated`, `FlowCallerNotMigrated` (`EPPlusReason`).
- A Manual helper is reported twice (`epplus.flow-types` and `epplus.unsupported`); harmless under AllOrNothing, could be deduplicated later.

## Steps

- [x] 1. `FlowTypesPattern` (`src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs`), between `epplus.package` and `epplus.unsupported`
- [x] 2. `WorkbookUses`: parameter constructor; worksheet local passed to a migratable helper accepted
- [x] 3. Proofs `src/Unshackle.Tests/Proofs/EPPlus/FlowTypes/`: `parameter` (after), `public-member`, `unfixed-use`, `unfixed-caller`, `method-group` (manual); `_Combined/parameter` (full fix + harness, added to `StylesCombinedProofTests`)
- [x] 4. KingmaDoc plan/models/verify `docs/projectinfo/patterns/epplus-flow-types-*`, README row
- [x] 5. Fields and return types: conservative, Manual `FlowFieldNotMapped` / `FlowReturnNotMapped` from `epplus.flow-types` (proofs `FlowTypes/field`, `FlowTypes/return`); Fixed shapes wait for council questions 2 and 3
- [x] 6. `AddressPattern` (`epplus.address`, after `epplus.dimension`): `ws.Cells[..].Rows/Columns` → `RowCount()/ColumnCount()`, `.Start/.End.Row/.Column` → `RangeAddress.FirstAddress/LastAddress.RowNumber/ColumnNumber`; `WorkbookUses` maps the receiver to `ws.Range(..)`. Proofs `Address/_Combined/rows-columns` (combined + harness), `single-cell`, `reversed-literal`, `start-cell` (manual, `UnmappedAddress`). KingmaDoc `epplus-address-*`, README row, queue row 14
- [ ] 7. Corpus re-measure (not run: disk space)

## Progress

- 2026-10-27 (taak-327b): steps 5 and 6. Harness finding: `Cells[5, 4, 2, 3]` is rows 2-5 / columns 3-4 in both libraries (corners sorted); `Cells["C5:A2"]` throws `ArgumentOutOfRangeException` in EPPlus 4.5.3.3 and not in ClosedXML, so a reversed literal is Manual. Proofs `unfixed-caller` (caller is now `package.Workbook.Worksheets[1]`, no field) and `method-group` (returns `Delegate`) adjusted so their single reason stays the one they prove. New reason codes `FlowFieldNotMapped`, `FlowReturnNotMapped`, `UnmappedAddress`.

## Council questions

1. E18 says "private/internal only". Built: private only. Internal needs every caller in the project (SymbolFinder over the solution, `InternalsVisibleTo` excluded). Is internal wanted now, and is `InternalsVisibleTo` to a test project a blocker or should the test project migrate with it (same E17 project gate)?
2. Fields: a private `ExcelWorksheet _sheet` field assigned in a constructor from a parameter crosses the constructor's callers. Which shape is in scope first: a field assigned only from a migrated local in the same method set, or none until the constructor flow is proven too?
3. Return types: `private ExcelWorksheet AddSheet(ExcelPackage p, string name) => p.Workbook.Worksheets.Add(name)` needs `ExcelPackage` as a parameter type, whose `Worksheets.Add` with a non-literal name is Manual today (invalid-name exception differs, harness finding c). Should a non-literal sheet name be accepted when the caller passes a literal (constant propagation through one call)?
4. Chains: a migrated helper passing its parameter to another helper is refused (no recursion). Allow one level, or a fixed depth like A11's 3?
5. Should `epplus.unsupported` skip a method `epplus.flow-types` already reported Manual (one row instead of two)? Same for a Manual `epplus.address` and a field/return now reported by both.
6. Step 5 was built conservatively: fields and EPPlus return types are Manual (`FlowFieldNotMapped`, `FlowReturnNotMapped`), not Fixed, while E18 asks for `field` and `return` after proofs. Which first Fixed shape is wanted (see questions 2 and 3)?
7. `epplus.address` maps only a range taken straight from `ws.Cells[..]`. A range in a local (`var r = ws.Cells[1, 1, n, 3]; r.Rows`) needs `ExcelRange` locals in `WorkbookUses`. Build that next, or first the `ExcelRange` parameter flow type?
8. `epplus.address` accepts any four int expressions, like the existing `Cells[..]` → `Range(..)` mapping: a zero or negative coordinate throws in both libraries but with different exception types. Accept as today, or Manual unless the ints are constants >= 1?

## See also

- `claude/tasks/COUNCIL-round-13.md` (E18)
- `claude/tasks/epplus-queue.md` row 13
- `claude/tasks/TAAK-309-epplus-to-closedxml.md`
- `docs/projectinfo/patterns/epplus-flow-types-plan.md`
