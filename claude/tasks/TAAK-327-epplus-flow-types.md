# TAAK-327 — EPPlus flow types (`epplus.flow-types`, council E18)

**Status**: step 1 (parameters) done; fields, return types and step 2 open
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
- [ ] 5. Fields and return types (after a council answer)
- [ ] 6. `ExcelAddressBase.Rows/Columns`, `Start/End` as a separate pattern with E1 harness cases (E18 step 2)
- [ ] 7. Corpus re-measure (not run: disk space)

## Council questions

1. E18 says "private/internal only". Built: private only. Internal needs every caller in the project (SymbolFinder over the solution, `InternalsVisibleTo` excluded). Is internal wanted now, and is `InternalsVisibleTo` to a test project a blocker or should the test project migrate with it (same E17 project gate)?
2. Fields: a private `ExcelWorksheet _sheet` field assigned in a constructor from a parameter crosses the constructor's callers. Which shape is in scope first: a field assigned only from a migrated local in the same method set, or none until the constructor flow is proven too?
3. Return types: `private ExcelWorksheet AddSheet(ExcelPackage p, string name) => p.Workbook.Worksheets.Add(name)` needs `ExcelPackage` as a parameter type, whose `Worksheets.Add` with a non-literal name is Manual today (invalid-name exception differs, harness finding c). Should a non-literal sheet name be accepted when the caller passes a literal (constant propagation through one call)?
4. Chains: a migrated helper passing its parameter to another helper is refused (no recursion). Allow one level, or a fixed depth like A11's 3?
5. Should `epplus.unsupported` skip a method `epplus.flow-types` already reported Manual (one row instead of two)?

## See also

- `claude/tasks/COUNCIL-round-13.md` (E18)
- `claude/tasks/epplus-queue.md` row 13
- `claude/tasks/TAAK-309-epplus-to-closedxml.md`
- `docs/projectinfo/patterns/epplus-flow-types-plan.md`
