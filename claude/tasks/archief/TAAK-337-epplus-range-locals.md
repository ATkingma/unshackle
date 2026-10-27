# TAAK-337 — EPPlus `ExcelRange` locals and parameter flow (council round 16 E25, E26)

**Status**: done
**Created**: 2026-10-27
**Goal**: let `epplus.address` and the range mappings follow a range held in a local, then an `ExcelRange` parameter.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-16.md` E25, E26. Background: `claude/tasks/TAAK-327-epplus-flow-types.md` step 6 and questions 7-8.
- Code: `src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs` (`IsRangeLocal`, `MapRangeRead`, range-helper arguments, range
  parameter constructor), `AddressPattern.cs` (`LocalSource`, `Mapped`, `InOrder`), `FlowTypesPattern.cs` (`RewriteRange`,
  `RangeBlocked`, `MigratableRange`), `EPPlusSyntax.IsRangeType`.
- E26: any int coordinates stay accepted (as `Cells[..]` → `Range(..)` today); only documented as a limitation.

## Steps

- [x] 1. `WorkbookUses`: an `ExcelRange` local (`var r = ws.Cells[...]`) assigned once from a mapped `Cells[..]` becomes a ClosedXML range local (`ws.Range(..)`); every use must be mapped, else Manual
- [x] 2. `AddressPattern`: `r.Rows/Columns/Start/End` on such a local. Proofs `Address/_Combined/range-local` (combined + harness), `Address/range-local-reassigned` (manual)
- [x] 3. `ExcelRange` as a private by-value parameter type, through `FlowTypesPattern.MigratableRange`. Proofs `FlowTypes/range-parameter` (after), `FlowTypes/_Combined/range-parameter` (combined + harness), `range-parameter-unfixed-use` (manual)
- [x] 4. E26: the non-positive coordinate limitation is in `docs/projectinfo/patterns/epplus-address-plan.md`
- [x] 5. KingmaDoc plan/models/verify updated for address and flow-types; `dotnet build`, `dotnet test`; archived

## Council questions

1. Uses of a range local or range parameter other than its address (a value assignment, style, `Merge()`, `InsertData`) are Manual for
   now, although `ws.Cells[..]` has those mappings. Conservative choice; extend per use when the corpus shows them?
   **Decision (round 19 R1)**: Manual stays, reason names the use; extend per use on corpus evidence.
2. A range parameter passed on to another helper is Manual (no range chains like E23 for worksheets). Needed?
   **Decision (round 19 R2)**: Manual accepted; reason names the callee.
3. A range local whose source is not `Cells[..]` straight (e.g. `ws.Cells` without indexer, or a range from another range) is Manual
   `UnmappedAddress`; these now get an `epplus.address` row where before only `epplus.unsupported` reported them.
   **Decision (round 19 R3)**: accepted, one row per node: fold into the `epplus.unsupported` row. TAAK-346 step 2a.

## See also

- `claude/tasks/COUNCIL-round-16.md`
- `claude/tasks/archief/TAAK-336-epplus-flow-followups.md`
- `docs/projectinfo/patterns/epplus-address-plan.md`
- `docs/projectinfo/patterns/epplus-flow-types-plan.md`
