# TAAK-282 — Generated mapping omits `required` members (CS9035)

**Status**: done 2026-10-18 (branch `worktree-agent-a7fb36f3b60c1eb0d`)
**Created**: 2026-10-18

## Context

Found in: encryptednotes (`BusinessLogic/Mapping/BusinessLogicMappingProfile.cs`: `BaseEntity.Id`, `NoteEntity.UserId`) and estore (`API/Extensions/MapperProfile.cs`: `Category.CategoryId`, `Member.MemberId`, `Order.OrderId`). Build fails after `fix` (never a silent test change).

The destination has `required` members that the source does not map. AutoMapper constructs the object by reflection and leaves them at `default`; the generated `new() { ... }` initializer must name every required member or the compiler rejects it (8 + 4 errors).

## Steps

1. [x] Failing proof: `src/Unshackle.Tests/Proofs/AutoMapper/_Combined/_Pending/required-members.before.cs` + `.after.cs` (parked in `_Pending`, which the proof runners skip; verified failing on `db05908` by copying it out of `_Pending`; the expected `after` text is provisional and may be adjusted).
2. [x] Fix: Add `Member = default` (`default!` for reference types) for every required destination member without a source, in the same place where unmapped members are left out (TAAK-261). Probe first that AutoMapper 14.0.0 really leaves them default and does not throw.
3. [x] Move the proof out of `_Pending`, `dotnet test` green, KingmaDoc plan/verify updated for the touched pattern.
4. [x] Corpus: rerun the repo with `claude/tools/corpus-am.sh` (`after`) and check 0 regressions.

## See also

- `claude/tasks/CORPUSRUN-blind.md` (round 5)
- `src/Unshackle.Core/Fixes/AutoMapper/`

## Context for the next agent

- Probe (AutoMapper 14.0.0, console program): an unmapped `required` member stays at `default` (`int` 0, `string` null, base-class member too), no exception; one with a property initializer keeps the initializer value; one assigned in a parameterless constructor keeps that value; a mapped one gets the source value. So AutoMapper ignores `required`.
- Done: in `CreateMapPattern.Problem`, after the member loop, every `required` member nothing maps gets `Member = default` (`default!` for a non-nullable reference type when nullable warnings are on), also in the ProjectTo expression (`read`). If the member has an initializer or the type declares a constructor (`KeepsDefault` false), the profile is Manual with a precise reason. Proofs `_Combined/required-members`, `_Combined/project-to-required`; unit tests `Rewrite_RequiredMember*`; KingmaDoc REQ-12.
- Corpus rerun (tool `_results/tool-a7fb36f`): encryptednotes, estore build after `fix`, 0 regressions; results under `/mnt/dikkeschijf/unshackle-corpus/corpus-blind/_results/after/`; CORPUSRUN-blind.md round 5 and README table updated.
