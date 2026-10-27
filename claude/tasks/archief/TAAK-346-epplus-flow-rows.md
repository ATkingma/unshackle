# TAAK-346 — EPPlus flow rows after council round 18

**Status**: done
**Created**: 2026-10-27
**Goal**: Implement council round 18 E27, E29, E31, E32 for `epplus.flow-types` (follow-up of TAAK-336).

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-18.md` (E27-E32). E28 and E30 need no work.
- Code: `src/Unshackle.Core/Fixes/EPPlus/` (flow types, `UnsupportedPattern`), proofs `src/Unshackle.Tests/Proofs/EPPlus/`.

## Steps

- [x] 1. E29: a rename in the caller to a sheet name that a return helper adds becomes Manual (proof + unit test)
- [x] 2. E31: when an E6 public-API signature applies to a flow-types node, fold the flow-types reason into the E6 row; one row per node, the summary counts it once (adapt `EPPlusFixTests.Fix_NodeLeftManualByFlowTypesOrAddress_OneRowPerNode` or add a test)
- [x] 2a. Round 19 R3: an `epplus.address` `UnmappedAddress` row on a node already reported by `epplus.unsupported` is folded into that row (one row per node, unit test); R1/R2 reason texts name the range use or callee
- [x] 3. E32: the Manual row of a linked helper set names the failing member (proof text)
- [x] 4. E27: name `_Combined/field` as the Fixed field proof in `docs/projectinfo/patterns/epplus-flow-types-plan.md`; update plan/models/verify for 1-3
- [x] 5. `dotnet build`, `dotnet test`; archive

## Result

- E29: `WorkbookUses.IsNameAccess` also rejects names return helpers add (`AddedByHelpers`, `FlowTypesPattern.ReturnedSheetName`); proof `Package/rename-to-helper-added`.
- E31: `FlowTypesPattern.Find` skips nodes with an E6 signature (`UnsupportedPattern.ExposedSignature`); the E6 row appends the flow-types reason. Test `EPPlusFixTests.Fix_FlowTypesNodeWithPublicApiSignature_OneE6RowWithFlowReason`; proof `FlowTypes/public-member` now uses an internal type.
- E32: the chain Manual row appends "Failing member X: <reason>"; proof `FlowTypes/chain-failing-member`.
- Step 2a (R3): `AddressPattern.Find` skips a Manual read when `UnsupportedPattern.ReportsBesides` the member; the unsupported row appends "Also epplus.address UnmappedAddress: ...". Test `EPPlusFixTests.Fix_AddressReadInMemberReportedByUnsupported_OneRowWithAddressReason`; proof `Unsupported/address-folded`. R1/R2: `WorkbookUses.RangeUse` names the use or callee; proofs `FlowTypes/range-parameter-unfixed-use`, `FlowTypes/range-parameter-passed-on`.
- E27 and REQ-15..17 in the KingmaDoc plan/verify.

## Council questions

1. E29 is conservative: any return-helper call on the package anywhere in the scope (also in a branch, or after the rename) blocks a rename to its name. Acceptable?

## See also

- `claude/tasks/COUNCIL-round-18.md`
- `claude/tasks/COUNCIL-round-19.md`
- `claude/tasks/archief/TAAK-336-epplus-flow-followups.md`
- `docs/projectinfo/patterns/epplus-flow-types-plan.md`
