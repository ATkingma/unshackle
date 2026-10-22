# TAAK-299 — Split CreateMapPattern.cs and Fixer.cs

**Status**: done (all four steps; push by the main session)
**Created**: 2026-10-21
**Goal**: execute the split proposal from TAAK-297 step 5: `CreateMapPattern.cs` (2040 lines) into five classes, `Fixer.cs` (966 lines) into five; behaviour and rendered output exactly the same.

## Context for the next agent

- The proposal with the line ranges per new class is in `claude/tasks/archief/TAAK-297-code-standards-cleanup.md` ("Proposal step 5"). Line numbers are from main `a1df338`; re-check them.
- Pure refactor: no test expectation may change. If one has to, stop and take it to the council (`claude/docs/council.md`).
- `FileFixer` is the seam TAAK-298 needs for parallel file processing (one file in, one `FilePass` out).
- Code standards: `claude/docs/code-standaarden.md` (one type per file, never `sealed`, `CancellationToken` required).
- Push procedure: `claude/docs/overdracht.md` "Push procedure".

## Steps

- [x] 1. Split `CreateMapPattern.cs`: `CreateMapProblems`, `ProfileSyntax`, `MapAccessibility`, `MapMethodWriter`; update callers (subclasses and other AutoMapper patterns)
- [x] 2. Split `Fixer.cs`: `PartialRun`, `FileFixer`, `SolutionChecks`, `FindingFactory`
- [x] 3. Update `docs/projectinfo/architectuur/core.md` (and class models that name the moved methods)
- [x] 4. Build, tests, sample tests green → commit (push by the main session)

## Progress log

- 2026-10-21: step 1. Line ranges re-checked against `b236685` (2037 lines after TAAK-295). Moved, unchanged apart from `private`/`protected` → `internal` and qualified calls:
  - `CreateMapProblems.cs` (1000 lines): old 582–1548 and 1568–1585 (`OutsideProblem` … `NullSafeChain`, `MayBeNull`, `BlockMarker`)
  - `ProfileSyntax.cs` (328 lines): old 527–530 (`MapStatements(constructor)`) and 1602–1910 (`Anchor`, `IsProfileWithMaps` … `IsProfile`)
  - `MapAccessibility.cs` (52 lines): old 1549–1567 (`IsReference`, `IsAvailableOutsideProfile`) and 1911–1928 (`Effective`, `Weakest`)
  - `MapMethodWriter.cs` (175 lines): old 531–581 (`ExtensionsName`, `Emptied`, `Trimmed`, `AtAnchor`) and 1929–2036 (`MethodName`, `Extensions`)
  - `CreateMapPattern.cs` keeps 543 lines: `Find`, the `Rewrite`s, `PlanAsync`, `SeveralConstructorsReason`, `AllMembers` (not in the proposal, used outside the problems too); `AutoMapperAssembly` became `internal`.
  - Callers updated in 14 AutoMapper pattern files (subclasses now call `ProfileSyntax.IsProfileWithMaps`/`UsesForMember`/`Anchor`); 18 docs in `docs/projectinfo` renamed `CreateMapPattern.X` to the new class.
  - Build 0 warnings, `dotnet test` 1652/1652, SampleShop 38/38. Commit: see `git log` ("TAAK-299: split CreateMapPattern").
- 2026-10-21: step 2. Line ranges re-checked against `58569c1` (966 lines, same as the proposal). Moved, unchanged apart from `private` -> `internal` and qualified calls; each new class is `internal static` in `Unshackle.Core`:
  - `PartialRun.cs` (131 lines): old 86–193 (`RunPartialAsync`, `KeepsKeptCodeAsync`, `InOriginalAsync`, `IsKeptPackageRow`) and `MaxPartialRounds`. `RunPartialAsync` was an instance method only to call `RunAsync`; it now takes that as a `run` delegate (`Fixer.FixAsync` passes `(s, seed, ct) => RunAsync(fix, s, seed, ct)`), and its then unused `fix` parameter went.
  - `FileFixer.cs` (371 lines): old 404–420 (`NewCode`, moved here because only `FixFileAsync` uses it), 422–603 (`FixFileAsync`, `FixWithTargetedImportsAsync`, `AddReferences`), 805–956 (`ApplyPatternAsync`, `BlockedCall`, `RewriteAsync`, `Replace`, `InnerDirectives`, `IsRegionDirective`, `ReplaceWithTrivia`) and `MaxTargetedRounds`.
  - `SolutionChecks.cs` (120 lines): old 693–797 (`SwapGlobalUsingsAsync`, `UnreachableFilesAsync`, `NotUtf8FilesAsync`).
  - `FindingFactory.cs` (42 lines): old 605–616, 799–803, 958–965 (`NotApplied`, the `CreateFinding` overloads, `CreateUsingsFinding`, `CreateProjectFinding`, `InternalError`).
  - `Fixer.cs` keeps 360 lines: constants, constructors, `FixAsync`, `RunAsync`, `FixCopyAsync` (merges a pass into the shared findings, so it stays with the loop), `PassCopiesAsync`, `MigrateProjectFilesAsync`. The reason constants (`KeptPackageReason`, `KeptSharedPackageReason`, `KeptGlobalUsingReason`, `InactiveBranchReason`) became `internal`.
  - Shared state that remains in `FileFixer` (for TAAK-298): `FixFileAsync`, `ApplyPatternAsync` and `FixWithTargetedImportsAsync` append to (the latter also truncates) the caller's `List<Finding> findings`; a parallel run needs a list per file, merged in file order. `swappedTypes` and `globalProjects` are `HashSet`s passed in and only read there (safe to share read-only; could become `IReadOnlySet`). `requiredNamespaces`, `blocked` and `leftOut` are local per call. `Solution`/`Document`/`SemanticModel` are immutable Roslyn snapshots; the `IFix`/`IPattern` instances are shared and must stay stateless. `Fixer.s_noneBlocked` is a shared empty read-only dictionary.
- 2026-10-21: step 3. `docs/projectinfo/architectuur/core.md` (file tree, method names), `modellen/fix-flow.md` (step locations), and `Fixer.X` references to moved methods in four feature/pattern docs and two comments in `UsingDirectives.cs`.
- 2026-10-21: step 4. Build 0 warnings, `dotnet test` 1652/1652, SampleShop 38/38. Task archived.

## See also

- `claude/tasks/archief/TAAK-297-code-standards-cleanup.md` — the proposal
- `claude/tasks/archief/TAAK-298-performance.md` — uses the `FileFixer` seam
- `claude/docs/INDEX.md` — documentation index
