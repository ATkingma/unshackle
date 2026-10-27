# TAAK-351 — EPPlus address row fold and helper-name reason

**Status**: open
**Created**: 2026-10-27
**Goal**: Implement council round 19 R3 (lost when TAAK-346 was archived without step 2a) and council round 20 E33.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-19.md` (R3), `claude/tasks/COUNCIL-round-20.md` (E33).
- Code: `src/Unshackle.Core/Fixes/EPPlus/Patterns/` (`AddressPattern`, `UnsupportedPattern`, `WorkbookUses.IsNameAccess`,
  `FlowTypesPattern.ReturnedSheetName`); the E31 fold from TAAK-346 is the model for R3.

## Steps

- [ ] 1. R3: an `epplus.address` `UnmappedAddress` row on a node already reported by `epplus.unsupported` is folded into that row (one row per node, summary counts it once; unit test)
- [ ] 2. E33: the Manual row for a rename to a helper-added sheet name names the helper call with file:line (update proof `Package/rename-to-helper-added`)
- [ ] 3. Update `docs/projectinfo/patterns/epplus-address-*` and `epplus-flow-types-*`; `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-20.md`
- `claude/tasks/archief/TAAK-346-epplus-flow-rows.md`
