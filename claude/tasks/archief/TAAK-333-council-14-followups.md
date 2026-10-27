# TAAK-333 — Council round 14 follow-ups (Moq report rows, add-side using rule, B1 Info)

**Status**: done
**Created**: 2026-10-27
**Goal**: implement decisions M10-M13 of `claude/tasks/COUNCIL-round-14.md`.

## Context for the next agent

TAAK-330 fixed the Moq corpus leftovers and left four questions; council round 14 answered them. All four need code. Code standards
and the "never push red" rule apply (`CLAUDE.md`).

## Steps

- [x] 1. M11: `ProjectMigration.KeptForDependents` (list, solution order; set by `ProjectPackages.KeepForDependents`, which now walks
      the references per keeping root). `Fixer.DependentList` names up to `MaxDependentsInRow` (5) paths, then "and N more";
      `ManualResult.Dependents` carries the full list and `ReportRow.Dependents` writes it to the JSON report (omitted when null).
      Tests: `Fixer_HelperKeptForSeveralDependents_RowCapsListAndResultKeepsAll` (2 and 7 dependents).
- [x] 2. M10: the kept-for-dependent row appends "; <package> was added next to it" when the project file edit added a package of
      `PackageMigration.AddedPackages` (taken from the fix). One row per project file stays. `PartialRun.IsKeptPackageRow` (now
      internal) still recognises it; asserted in `Fixer_HelperKeptForDependent_ReportNamesDependentAndOneRowPerProjectFile`.
- [x] 3. M12: `UsingDirectives.UpdateAsync` drops a missing namespace of the fix (`IFix.AddedNamespaces` only, not pattern-required
      namespaces, which may serve generated code that binds later) when the file writes a full name from the root and no simple name
      may need it: no unqualified reference into it, no swapped type written unqualified, no unbound or ambiguous simple name
      (conservative). Changed proofs: `Proofs/MediatR/QualifiedSwappedType/record-base.after.cs`,
      `Proofs/MediatR/_Combined/qualified-request-afripay.after.cs` (both lose `using Mediator;`). Unit test:
      `SwappedTypesTests.FixAsync_QualifiedSwappedType_AddsUsingOnlyForUnqualifiedReference` (qualified-only and mixed).
- [x] 4. M13: `IFix.ChangedCodeInfo` (default null); `MoqFix.ProxyInfo` is emitted once per run that changed code as an Info row
      (pattern id `changed-code-info`, on the solution file). Documented in `docs/projectinfo/patterns/moq-create-plan.md` (Risks).
      Tests: `Fixer_ChangedCode_OneProxyInfoRow`, `Fixer_NothingChanged_NoProxyInfoRow`; `MoqRuntimeProofTests` allows the row.
- [x] 5. `dotnet build`, `dotnet test` (3807 green), sample tests (38 green). Not pushed (agent worktree; the caller merges).

## See also

- `claude/tasks/COUNCIL-round-14.md`
- `claude/tasks/TAAK-330-moq-corpus-leftovers.md`
- `claude/tasks/CORPUSRUN-moq.md`
- `claude/docs/council.md`
