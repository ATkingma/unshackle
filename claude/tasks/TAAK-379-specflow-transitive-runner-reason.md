# TAAK-379 — SharedDeclarationBlocked reason is wrong for a runner package that arrives transitively

**Status**: open
**Created**: 2026-10-28
**Goal**: When `SpecFlow.NUnit` reaches a project through another NuGet package, the Manual row says so and names that package.

## Context for the next agent

- Corpus round 1 (`claude/tasks/CORPUSRUN-specflow.md`): `ais-dotnet/Ais.Net` and `corvus-dotnet/Corvus.Retry` reference only
  `Corvus.Testing.SpecFlow.NUnit` 1.6.0, which brings `SpecFlow.NUnit` transitively.
- The row reads "an imported file, a conditional import or a property-driven name brings it in; rename it ... there by hand". None of
  that applies; the user must replace the wrapper package (a Reqnroll build of it, if any). Blocking is right (S9), the text is not.
- No proof: proofs cannot model package dependencies. Unit test on the reason with a fake `IProjectProperties` (reference path from a
  package that is not declared, while another declared package depends on it).

## Steps

- [ ] 1. Distinguish "declared nowhere, comes from package X's dependencies" (from `project.assets.json` or the reference path) from the import cases
- [ ] 2. Reworded reason naming the wrapper package; unit test

## See also

- `claude/tasks/CORPUSRUN-specflow.md`
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md` — question 9 (S9, S10)
