# TAAK-112 — Bogus does not load in Unshackle while `dotnet build` succeeds

**Status**: done (2026-10-11)
**Created**: 2026-10-11 (corpus run, `claude/tasks/CORPUSRUN-fa.md` follows)

## Context

Bogus: `fix` stops with "The solution could not be loaded (6 errors)": 3× "Builder: The type or namespace name 'System' could not be found" and NU1701 (Rant 3.0.530, Z.ExtensionMethods.WithTwoNamespace, net4x packages in a net9.0 project) in Bogus.Tests.csproj. TAAK-090 catches NU1701 as a warning, but these messages come in as an MSBuild failure with a different text/shape.

## Steps

1. Reproduce on the Bogus copy (scratchpad/run-fa/src/bchavez_Bogus).
2. Treat NU1701 in this form ('Msbuild failed when processing … Package … was restored using …') as a warning.
3. 'Builder': probably a build project (NUKE etc.) that was not restored or contains no tests/FA — investigate; projects without an FA reference that do not load may give a warning instead of being fatal (justify, see TAAK-090 rules).
4. Evidence: Bogus loads, fix + build green.

## Result

- NU1701 in the form "Msbuild failed when processing … was restored using …" was already a warning (`IsTolerable`: project loaded).
  Only the CS0246 check was fatal: 'Builder' (NUKE) is not restored by `dotnet restore Bogus.sln` (no
  `project.assets.json`) and the design-time build cannot even find `System`.
- Fix (`src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs`): a project in which `System.Object` does not resolve, while
  other projects do resolve, and on which no project depends, is left out of the solution with a warning. A single
  unrestored project or a project that others depend on stays fatal (the existing CS0246 check).
- Test: `SampleShopTests.Load_LeafProjectWithoutSystemNextToRestoredOne_LeavesItOutWithWarning` (simulated with
  `DisableImplicitFrameworkReferences`; an unrestored project with packages does find System in the test).
- Corpus: fresh copy of Bogus (`scratchpad/v110/work/bogus`): loads, 865 Fixed / 113 Manual, **build green**.

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
