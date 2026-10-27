# TAAK-333 — Council round 14 follow-ups (Moq report rows, add-side using rule, B1 Info)

**Status**: open
**Created**: 2026-10-27
**Goal**: implement decisions M10-M13 of `claude/tasks/COUNCIL-round-14.md`.

## Context for the next agent

TAAK-330 fixed the Moq corpus leftovers and left four questions; council round 14 answered them. All four need code. Code standards
and the "never push red" rule apply (`CLAUDE.md`).

## Steps

- [ ] 1. M11: `ProjectMigration.KeptForDependent` becomes a list of all keeping dependents in solution order (set by
      `ProjectPackages.KeepForDependents`). `Fixer.KeptForDependentReason` lists up to 5 paths, then "and N more"; the JSON report
      keeps the full list. Tests: helper kept for 2 and for 7 dependents.
- [ ] 2. M10: when the new package was added to a kept project, the kept row's reason appends "; NSubstitute was added next to it."
      (package name taken from the fix, not hard-coded). Still one row per project file. Extend
      `Fixer_HelperKeptForDependent_ReportNamesDependentAndOneRowPerProjectFile`; `PartialRun.IsKeptPackageRow` must still recognise the row.
- [ ] 3. M12: apply the full-name-from-root rule of `UsingDirectives.IsMemberOfReceiver` to the add-new-usings decision too: a using
      is added only when a rewritten reference in the file is unqualified. Update proof `Proofs/MediatR/QualifiedSwappedType` and any
      other `after` that changes; list them here. Unit tests: qualified-only swap adds no using; mixed swap adds it.
- [ ] 4. M13: the Moq fix emits one fix-level Info line when it changed code (text in COUNCIL-round-14 M13). Mention it in the
      moq.create pattern docs (`docs/projectinfo/patterns/`). Report test for the Info line.
- [ ] 5. `dotnet build`, `dotnet test`, sample tests green; push and watch CI.

## See also

- `claude/tasks/COUNCIL-round-14.md`
- `claude/tasks/TAAK-330-moq-corpus-leftovers.md`
- `claude/tasks/CORPUSRUN-moq.md`
- `claude/docs/council.md`
