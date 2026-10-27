# TAAK-336 — EPPlus flow-type follow-ups (council round 16 E21-E24)

**Status**: open
**Created**: 2026-10-27
**Goal**: build the first Fixed field and return shapes, helper chains, and one row per node for `epplus.flow-types`.

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-16.md` E20-E24. Background: `claude/tasks/TAAK-327-epplus-flow-types.md`.
- Code: `src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs` (`Migratable` is the shared caller/callee check), `WorkbookUses.cs`,
  `PackagePattern.cs`, `UnsupportedPattern.cs`; reason codes in `EPPlusReason`.
- Private only (E20); keep `FlowMemberNotPrivate` for internal/public members.
- Overlaps TAAK-337 in `WorkbookUses.cs` and `FlowTypesPattern.cs`: run this one first, or use separate worktrees.

## Steps

- [ ] 1. E21 field: a private field of a non-partial type, every write inside the type from a migrated package/worksheet expression, no constructor parameter source → Fixed. A constructor parameter source stays `FlowFieldNotMapped`. Rename the current proof `Proofs/EPPlus/FlowTypes/field` to `field-ctor-parameter` if it has that shape, add `field` (after) and a `_Combined` harness case
- [ ] 2. E22 return: a private helper whose return expression is Fixed (literal sheet name or index) with all callers migrated → Fixed; a non-literal name stays `FlowReturnNotMapped`. Proofs `return` (after), `return-nonliteral-name` (manual)
- [ ] 3. E23 chains: a helper passing its parameter to another private helper, depth <= 3; a cycle or deeper → Manual `FlowUseNotMigrated`. Proofs `chain` (after), `chain-recursive` (manual)
- [ ] 4. E24: `UnsupportedPattern` skips a node already reported Manual by an earlier EPPlus pattern (`epplus.flow-types`, `epplus.address`); unit test asserting one row per node
- [ ] 5. KingmaDoc `docs/projectinfo/patterns/epplus-flow-types-*` plan/models/verify updated (new shapes, E20 private-only note)
- [ ] 6. `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-16.md`
- `claude/tasks/TAAK-327-epplus-flow-types.md`
- `claude/tasks/TAAK-337-epplus-range-locals.md`
