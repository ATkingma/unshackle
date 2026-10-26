# TAAK-326 — EPPlus per-project migration (council round 13, E17)

**Status**: implementation, tests, sample and docs done; corpus re-measure round 2 in `CORPUSRUN-epplus.md`
**Created**: 2026-10-26
**Goal**: An EPPlus project with 0 blocking rows migrates even when another project in the solution stays on EPPlus.

## Context for the next agent

- Decision E17 (`claude/tasks/COUNCIL-round-13.md`): per project, never per file; a project migrates only with 0 blocking rows and when no EPPlus
  type it declares or exposes is used by a project that stays on EPPlus (else Manual `EpplusTypeCrossesProject`). EPPlus is removed only when
  nothing in the project uses it. No new CLI flag. E7 per migrated project.
- Round 1 (`CORPUSRUN-epplus.md`): 0 Fixed over 38 repos, because AllOrNothing undid every repo with a single Manual row.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Fixes/EPPlus/EPPlusProjectPlan.cs` (new), `EPPlusFix` (now `IPartialFix`), `EPPlusReason.EpplusTypeCrossesProject`; engine: `PartialPlan.KeptProjects`, `Fixer.RunAsync`, `PartialRun` |
| **How**   | `EPPlusFix.PlanPartialAsync` keeps every project holding a root, then closes over shared files and project references (EPPlus-typed members a kept project binds to; a kept project that is not proven to declare EPPlus itself). The plan annotates the kept documents (no pattern sees them) and names the kept projects; the Fixer treats them like G4-blocked projects (TAAK-320/321): original text, no package edit, row `EPPlus package kept: blocked by <reason>`. |
| **Why**   | A worksheet can cross a method boundary inside a project (not provable per file), but only a member signature crosses a project boundary, which is checkable. |

Engine change (generic, minimal): `PartialPlan.KeptProjects` (path → reason, default empty). `Fixer.RunAsync` merges it into the blocked
projects/files; `PartialRun` does not count rows in kept projects as new blockers. Other fixes are unaffected (empty by default).

## Steps

- [x] 1. Planner + engine hook
- [x] 2. Unit tests `EPPlusPerProjectTests` (clean + blocked, all blocked, crossing type, kept project referencing a clean one with/without declared EPPlus, shared file)
- [x] 3. Proof `Proofs/EPPlus/_Combined/PerProject` (clean.after.cs runs through the harness too), sample `samples/EPPlusSplit` + integration test `SampleShopTests.Fix_EPPlusSplit_MigratesOnlyTheCleanProject`
- [x] 4. KingmaDoc: `epplus-package-plan.md` (Design, REQ-22), `-modellen.md` (figure 4), `-verify.md`; `epplus-unsupported-plan.md`
- [ ] 5. Corpus re-measure (round 2) with after-build/test for repos with migrated projects

## Progress log

- 2026-10-26: steps 1-4. `dotnet build` 0 warnings; `dotnet test` green (see report).

## See also

- `claude/tasks/TAAK-309-epplus-to-closedxml.md`, `claude/tasks/epplus-queue.md`, `claude/tasks/CORPUSRUN-epplus.md`
- `docs/projectinfo/patterns/epplus-package-plan.md`
- `claude/docs/INDEX.md`
