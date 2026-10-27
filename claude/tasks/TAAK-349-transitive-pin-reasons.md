# TAAK-349 — Transitive pin reasons after council round 19

**Status**: open
**Created**: 2026-10-27
**Goal**: Implement council round 19 T4 and T5 for the transitive pin rows (follow-up of TAAK-343).

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-19.md` (T4, T5).
- Code: `src/Unshackle.Core/TransitivePackages.cs`, `src/Unshackle.Core/Fixer.cs`, tests
  `src/Unshackle.Tests/Core/Engine/TransitivePackagesTests.cs`.

## Steps

- [ ] 1. T4: keep any differing version Manual; reason says "higher: NU1605 risk" or "lower: removing the pin downgrades to X" (unit test per case)
- [ ] 2. T5: "kept for" lists all user projects in solution order, capped at three plus "and N more" (unit test with four users)
- [ ] 3. Update `docs/projectinfo/architectuur/core.md`; `dotnet build`, `dotnet test`; archive

## See also

- `claude/tasks/COUNCIL-round-19.md`
- `claude/tasks/archief/TAAK-343-transitive-pin-followups.md`
