# TAAK-336 — EPPlus flow-type follow-ups (council round 16 E21-E24)

**Status**: done
**Created**: 2026-10-27
**Goal**: build the first Fixed field and return shapes, helper chains, and one row per node for `epplus.flow-types`.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-16.md` E20-E24. Background: `claude/tasks/TAAK-327-epplus-flow-types.md`.
- Code: `src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs` (`Chain` is the shared caller/callee check behind `Migratable`;
  `MigratableField`, `MigratableReturn`), `WorkbookUses.cs` (field write, return-helper argument, chain argument),
  `UnsupportedPattern.cs` (E24); reason codes in `EPPlusReason`.
- Private only (E20); `FlowMemberNotPrivate` stays for internal/public members.
- Overlaps TAAK-337 in `WorkbookUses.cs` and `FlowTypesPattern.cs`.

## Steps

- [x] 1. E21 field: Fixed for a private worksheet field written only as `_f = p.Workbook.Worksheets.Add("x")` (or a proven lookup) of a
  package local in that local's scope; other uses are Name reads or later in a write's scope. Proof `field` renamed `field-ctor-parameter`
  (manual); the Fixed proof is `_Combined/field` (a single-pattern after proof cannot show a migrated write, see Council questions)
- [x] 2. E22 return: `private static ExcelWorksheet AddSheet(ExcelPackage p) => p.Workbook.Worksheets.Add("literal")` → `IXLWorksheet AddSheet(XLWorkbook p)`;
  `PackagePattern` accepts the package passed to it. Proofs `return` (after), `return-nonliteral-name` (manual), `_Combined/return`
- [x] 3. E23 chains: helpers linked by passing the worksheet parameter flip together, acyclic and at most 3 methods. Proofs `chain` (after),
  `chain-recursive`, `chain-too-deep` (manual), `_Combined/chain`
- [x] 4. E24: `UnsupportedPattern` skips nodes `epplus.flow-types` / `epplus.address` still find (left Manual), except an E6 public-API
  signature. Unit test `EPPlusFixTests.Fix_NodeLeftManualByFlowTypesOrAddress_OneRowPerNode`. The `Unsupported/cells`, `chart`, `internal-api`
  proofs and two `*UnsupportedTests` moved to a constructor / void method so they still show an unsupported row
- [x] 5. KingmaDoc `docs/projectinfo/patterns/epplus-flow-types-*` plan/models/verify updated (REQ-9..12, E20 note)
- [x] 6. `dotnet build`, `dotnet test`; archive

## Council questions

1. **Field after proof**: a single-pattern proof runs only flow-types on code that compiles against EPPlus, so a field write is never
   migrated there; the Fixed field is proven by `_Combined/field` (full fix + harness) only. Conservative; acceptable?
   **Decision (council 18 E27: accepted; plan names `_Combined/field` (TAAK-346).)**
2. **E22 index/lookup**: "literal sheet name or index" — only `Add("literal")` is mapped. A lookup by name or index inside the helper cannot
   prove the caller's package has that sheet (EPPlus returns null, ClosedXML throws), so it stays `FlowReturnNotMapped`.
   **Decision (council 18 E28: accepted.)**
3. **E22 names in the caller**: a sheet added by a return helper is not known to the caller's `AddedBefore`, so a later lookup of that name in
   the caller stays Manual (conservative); a rename to that name is not checked against it (both libraries throw on a duplicate name).
   **Decision (council 18 E29: a rename to a helper-added name becomes Manual too, with a test (TAAK-346).)**
4. **E21 field uses**: other uses must be in a write's package scope and after the write (top level of that scope); reads in other methods
   are Name reads only, no flow arguments. Widen later if the corpus shows it.
   **Decision (council 18 E30: accepted; widen on corpus evidence.)**
5. **E24 and E6**: a flow-types node whose signature is E6 public API of a packable project keeps its `PublicApiExposesOldLibrary`
   unsupported row (chosen as the more specific reason), so such a node can still have two rows.
   **Decision (council 18 E31: one row per node; flow-types reason folded into the E6 row (TAAK-346).)**
6. **E23 depth**: depth counts methods on the longest caller→callee path of the linked set (A→B→C = 3); one failing member makes the whole
   linked set Manual.
   **Decision (council 18 E32: accepted; the row names the failing member (TAAK-346).)**

## See also

- `claude/tasks/COUNCIL-round-18.md` — decisions E27-E32
- `claude/tasks/archief/TAAK-346-epplus-flow-rows.md` — follow-up
- `claude/tasks/COUNCIL-round-16.md`
- `claude/tasks/TAAK-327-epplus-flow-types.md`
- `claude/tasks/archief/TAAK-337-epplus-range-locals.md`
- `docs/projectinfo/patterns/epplus-flow-types-plan.md`
