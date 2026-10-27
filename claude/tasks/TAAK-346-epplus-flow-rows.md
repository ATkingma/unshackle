# TAAK-346 — EPPlus flow rows after council round 18

**Status**: open
**Created**: 2026-10-27
**Goal**: Implement council round 18 E27, E29, E31, E32 for `epplus.flow-types` (follow-up of TAAK-336).

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-18.md` (E27-E32). E28 and E30 need no work.
- Code: `src/Unshackle.Core/Fixes/EPPlus/` (flow types, `UnsupportedPattern`), proofs `src/Unshackle.Tests/Proofs/EPPlus/`.

## Steps

- [ ] 1. E29: a rename in the caller to a sheet name that a return helper adds becomes Manual (proof + unit test)
- [ ] 2. E31: when an E6 public-API signature applies to a flow-types node, fold the flow-types reason into the E6 row; one row per node, the summary counts it once (adapt `EPPlusFixTests.Fix_NodeLeftManualByFlowTypesOrAddress_OneRowPerNode` or add a test)
- [ ] 3. E32: the Manual row of a linked helper set names the failing member (proof text)
- [ ] 4. E27: name `_Combined/field` as the Fixed field proof in `docs/projectinfo/patterns/epplus-flow-types-plan.md`; update plan/models/verify for 1-3
- [ ] 5. `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-18.md`
- `claude/tasks/archief/TAAK-336-epplus-flow-followups.md`
- `docs/projectinfo/patterns/epplus-flow-types-plan.md`
