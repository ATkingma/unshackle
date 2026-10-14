# TAAK-073 — An unused `using FluentAssertions;` stays when the package goes

**Status**: completed
**Created**: 2026-10-11
**Found in**: corpus run Ardalis.Result (`docs/projectinfo/architectuur/corpus.md`)

## Problem

In `Ardalis.Result.AspNetCore.UnitTests` 2 files have `using FluentAssertions;` without a single assertion. Those files
give no findings, so the using swap does not run on them. The project has no other Manual, so `project-file`
removes the FluentAssertions reference → CS0246, the project no longer builds.

## What, where, how

- **Where**: the usings step in the `Fixer` / project-file step (no pattern code).
- **How**: if the fix removes the package from a project, also remove unused `using FluentAssertions*;` (and
  `global using`) from all files of that project, also without findings.
- No proof possible (proofs require ≥1 finding); a test in `src/Unshackle.Tests/Core/` with an in-memory solution.

## Steps

- [x] Clean up usings in files without findings on package removal
- [x] Unit test
- [x] `dotnet build` + `dotnet test` green

## Result

The `Fixer` also runs the using step for files without findings with an unused old using; without a change or swapped types it is only removed (no Shouldly using). Test: `FixerTests.FixAsync_FileWithoutFindingsHasUnusedRemovedUsing_RemovesItWithoutAddingNamespaces`.

## See also

- `docs/projectinfo/architectuur/corpus.md`
- `claude/tasks/archief/TAAK-044-fa-global-using.md`
