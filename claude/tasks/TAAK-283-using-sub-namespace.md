# TAAK-283 — A `using FluentAssertions.Common;` (sub-namespace of FluentAssertions) survives the fix (CS0246)

**Status**: open (regression found in blind round 5, TAAK-276; not fixed there)
**Created**: 2026-10-18

## Context

Found in: opentws (`OpenTws.IntegrationTests/InteractiveBrokers/Controllers/TwsControllerBaseTests.cs`). Build fails after `fix` (never a silent test change).

Only the exact namespace `FluentAssertions` is swapped; usings of its sub-namespaces (`FluentAssertions.Common`, `.Extensions`, ...) stay, but the package reference is removed, so the project no longer compiles. The using is unused in this file.

## Steps

1. [x] Failing proof: `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/_Pending/using-sub-namespace.before.cs` + `.after.cs` (parked in `_Pending`, which the proof runners skip; verified failing on `db05908` by copying it out of `_Pending`; the expected `after` text is provisional and may be adjusted).
2. [ ] Fix: Remove an unused `using FluentAssertions.*;`; when a type from the sub-namespace is used, the findings must be Manual so the package reference stays (check what `project-file` does today for such uses).
3. [ ] Move the proof out of `_Pending`, `dotnet test` green, KingmaDoc plan/verify updated for the touched pattern.
4. [ ] Corpus: rerun the repo with `claude/tools/corpus-am.sh` (`after`) and check 0 regressions.

## See also

- `claude/tasks/CORPUSRUN-blind.md` (round 5)
- `src/Unshackle.Core/UsingDirectives.cs`
