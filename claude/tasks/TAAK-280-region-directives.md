# TAAK-280 — AutoMapper profile with `#region` blocks breaks the build (CS1028)

**Status**: open (regression found in blind round 5, TAAK-276; not fixed there)
**Created**: 2026-10-18

## Context

Found in: assistiva (`AppUI/Utilities/AutoMapperProfile.cs`). Build fails after `fix` (never a silent test change).

A profile whose `CreateMap` statements sit inside `#region`/`#endregion` pairs: the rewrite removes the statements but leaves unbalanced directives (a stray `#endregion` ends up inside the new extension class), so the build fails with `CS1028: Unexpected preprocessor directive`.

## Steps

1. [x] Failing proof: `src/Unshackle.Tests/Proofs/AutoMapper/_Combined/_Pending/region-directives.before.cs` + `.after.cs` (parked in `_Pending`, which the proof runners skip; verified failing on `db05908` by copying it out of `_Pending`; the expected `after` text is provisional and may be adjusted).
2. [ ] Fix: Remove the statements together with the `#region`/`#endregion` lines that enclose only removed statements (or keep both lines of a pair); never one without the other. Also check `#if` around a `CreateMap`.
3. [ ] Move the proof out of `_Pending`, `dotnet test` green, KingmaDoc plan/verify updated for the touched pattern.
4. [ ] Corpus: rerun the repo with `claude/tools/corpus-am.sh` (`after`) and check 0 regressions.

## See also

- `claude/tasks/CORPUSRUN-blind.md` (round 5)
- `src/Unshackle.Core/Fixes/AutoMapper/`
