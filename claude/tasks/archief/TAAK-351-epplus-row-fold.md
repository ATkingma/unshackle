# TAAK-351 — EPPlus address row fold and helper-name reason

**Status**: done
**Created**: 2026-10-27
**Goal**: Implement council round 19 R3 (lost when TAAK-346 was archived without step 2a) and council round 20 E33.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-19.md` (R3), `claude/tasks/COUNCIL-round-20.md` (E33).
- Code: `src/Unshackle.Core/Fixes/EPPlus/Patterns/` (`AddressPattern`, `UnsupportedPattern`, `WorkbookUses.IsNameAccess`,
  `FlowTypesPattern.ReturnedSheetName`); the E31 fold from TAAK-346 is the model for R3.

## Steps

- [x] 1. R3: an `epplus.address` `UnmappedAddress` row on a node already reported by `epplus.unsupported` is folded into that row (one row per node, summary counts it once; unit test) — already done by TAAK-346b, see `claude/tasks/archief/TAAK-346-epplus-flow-rows.md` step 2a
- [x] 2. E33: the Manual row for a rename to a helper-added sheet name names the helper call with file:line (update proof `Package/rename-to-helper-added`)
- [x] 3. Update `docs/projectinfo/patterns/epplus-address-*` and `epplus-flow-types-*`; `dotnet build`, `dotnet test`; archive

## Result

- R3: no code change; done by TAAK-346 step 2a (test `EPPlusFixTests.Fix_AddressReadInMemberReportedByUnsupported_OneRowWithAddressReason`, proof `Unsupported/address-folded`).
- E33: `WorkbookUses.HelperClash` appends `the name "X" is already added by the helper call '...' (file:line)` to the worksheet Manual row; proof `Package/rename-to-helper-added` checks it; verify REQ-15 updated.

## See also

- `claude/tasks/COUNCIL-round-20.md`
- `claude/tasks/archief/TAAK-346-epplus-flow-rows.md`
