# TAAK-349 — Transitive pin reasons after council round 19

**Status**: done
**Created**: 2026-10-27
**Goal**: Implement council round 19 T4 and T5 for the transitive pin rows (follow-up of TAAK-343).

## Context for the next agent

- Decisions: `claude/tasks/COUNCIL-round-19.md` (T4, T5).
- Code: `src/Unshackle.Core/TransitivePackages.cs`, `src/Unshackle.Core/Fixer.cs`, tests
  `src/Unshackle.Tests/Core/Engine/TransitivePackagesTests.cs`.

## Steps

- [x] 1. T4: keep any differing version Manual; reason says "higher: NU1605 risk" or "lower: removing the pin downgrades to X" (unit test per case)
- [x] 2. T5: "kept for" lists all user projects in solution order, capped at three plus "and N more" (unit test with four users)
- [x] 3. Update `docs/projectinfo/architectuur/core.md`; `dotnet build`, `dotnet test`; archive

## Result

- `TransitivePackages.Direction` adds the T4 case to the Manual reason; `PackageAddEdit.KeptFor` is now a list, rendered by
  `PackageAddEdit.UserList` (max three plus "and N more"); the full list goes in `FixedResult.Dependents` / `ManualResult.Dependents`.

## Council questions

1. An unparsable reference bound gets no direction text (conservative). OK?
   **Decision (council 20 T6: accepted, no work.)**

## See also

- `claude/tasks/COUNCIL-round-19.md`
- `claude/tasks/COUNCIL-round-20.md`
- `claude/tasks/archief/TAAK-343-transitive-pin-followups.md`
