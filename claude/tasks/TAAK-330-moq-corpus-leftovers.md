# TAAK-330 — Moq corpus leftovers (B1, B2 report, qualified using)

**Status**: steps 1-4 done, step 5 open
**Created**: 2026-10-27
**Goal**: close the open ends of `CORPUSRUN-moq.md` round 2.

## Context for the next agent

- B2 (ocelot) was fixed by `ProjectPackages.KeepForDependents`, but the helper's report row said "is still used in this project"
  (wrong: only a dependent uses it), and a kept project got two project-file rows (Manual kept + Fixed edit).
- A `Moq.Mock<T>` written with its full namespace counted as a use of `Moq`, so `using Moq;` stayed (IDE0005 risk like B3).

## Steps

- [x] 1. Report text: `ProjectMigration.KeptForDependent` (set by `KeepForDependents`, first keeping dependent in solution order);
      `Fixer.KeptForDependentReason` row "Moq stays in this project because a project that references it still uses it: <path>".
      `PartialRun.IsKeptPackageRow` recognises the new reason.
- [x] 2. Duplicate rows: `Fixer.MigrateProjectFilesAsync` skips the Fixed change row of a project that has a kept row
      (shared-file rows with `KeptRemovedPackageFor` stay). Test `Fixer_HelperKeptForDependent_ReportNamesDependentAndOneRowPerProjectFile`.
- [x] 3. `UsingDirectives.IsMemberOfReceiver`: a type named by a qualifier that is its full name from the root (`Moq.Mock<T>`,
      `Moq.It`) needs no using. Only in the keep-or-remove decision of removed usings (`UsedRemovedNamespaces`); the "add new usings"
      decisions are unchanged (proof `MediatR/QualifiedSwappedType` relies on them). Test `Fixer_QualifiedMockOnManualFinding_RemovesUnusedMoqUsing`.
- [x] 4. B1: not Manual-detectable; documented as accepted limitation in `CORPUSRUN-moq.md`.
- [ ] 5. Corpus rerun (jellyfin, ocelot, orchardcore, radarr, fluentmigrator) with a tool built from this branch: not done (time).

## Council questions

1. Duplicate rows: the kept row now hides the Fixed edit row (the new package added next to the old one). Is a single Manual row per
   project file right, or should the kept row mention the added package?
2. A helper kept for several dependents names only the first (solution order). Enough, or list all?
3. Fully-qualified-name rule only applies to removing a removed using, not to adding the new one (a swapped `MediatR.IRequest` still
   brings `using Mediator;`). Should the add side follow too (would change MediatR proofs)?
4. B1 as accepted limitation (no Manual/Info row for `.Object` flowing out). Confirm.

## See also

- `claude/tasks/CORPUSRUN-moq.md`
- `claude/tasks/TAAK-307-moq-to-nsubstitute.md`
