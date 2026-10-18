# TAAK-282 — Generated mapping omits `required` members (CS9035)

**Status**: open (regression found in blind round 5, TAAK-276; not fixed there)
**Created**: 2026-10-18

## Context

Found in: encryptednotes (`BusinessLogic/Mapping/BusinessLogicMappingProfile.cs`: `BaseEntity.Id`, `NoteEntity.UserId`) and estore (`API/Extensions/MapperProfile.cs`: `Category.CategoryId`, `Member.MemberId`, `Order.OrderId`). Build fails after `fix` (never a silent test change).

The destination has `required` members that the source does not map. AutoMapper constructs the object by reflection and leaves them at `default`; the generated `new() { ... }` initializer must name every required member or the compiler rejects it (8 + 4 errors).

## Steps

1. [x] Failing proof: `src/Unshackle.Tests/Proofs/AutoMapper/_Combined/_Pending/required-members.before.cs` + `.after.cs` (parked in `_Pending`, which the proof runners skip; verified failing on `db05908` by copying it out of `_Pending`; the expected `after` text is provisional and may be adjusted).
2. [ ] Fix: Add `Member = default` (`default!` for reference types) for every required destination member without a source, in the same place where unmapped members are left out (TAAK-261). Probe first that AutoMapper 14.0.0 really leaves them default and does not throw.
3. [ ] Move the proof out of `_Pending`, `dotnet test` green, KingmaDoc plan/verify updated for the touched pattern.
4. [ ] Corpus: rerun the repo with `claude/tools/corpus-am.sh` (`after`) and check 0 regressions.

## See also

- `claude/tasks/CORPUSRUN-blind.md` (round 5)
- `src/Unshackle.Core/Fixes/AutoMapper/`
