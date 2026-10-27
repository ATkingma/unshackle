# TAAK-337 — EPPlus `ExcelRange` locals and parameter flow (council round 16 E25, E26)

**Status**: open
**Created**: 2026-10-27
**Goal**: let `epplus.address` and the range mappings follow a range held in a local, then an `ExcelRange` parameter.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-16.md` E25, E26. Background: `claude/tasks/TAAK-327-epplus-flow-types.md` step 6 and questions 7-8.
- Code: `src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs`, `AddressPattern.cs`, `FlowTypesPattern.cs`.
- E26: any int coordinates stay accepted (as `Cells[..]` → `Range(..)` today); only document the limitation.
- Overlaps TAAK-336 in `WorkbookUses.cs` and `FlowTypesPattern.cs`: do TAAK-336 first, or use separate worktrees.

## Steps

- [ ] 1. `WorkbookUses`: an `ExcelRange` local (`var r = ws.Cells[...]`) assigned once from a mapped `Cells[..]` becomes a ClosedXML range local (`ws.Range(..)`); every use must be mapped, else Manual
- [ ] 2. `AddressPattern`: `r.Rows/Columns/Start/End` on such a local. Proofs `Address/range-local` (after, combined + harness), `range-local-reassigned` (manual)
- [ ] 3. `ExcelRange` as a private by-value parameter type, through `FlowTypesPattern.Migratable`. Proofs `FlowTypes/range-parameter` (after) with a harness case, `range-parameter-unfixed-use` (manual)
- [ ] 4. E26: add the non-positive coordinate limitation to `docs/projectinfo/patterns/epplus-address-plan.md`
- [ ] 5. KingmaDoc models/verify updated for address and flow-types; `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-16.md`
- `claude/tasks/TAAK-336-epplus-flow-followups.md`
- `docs/projectinfo/patterns/epplus-address-plan.md`
