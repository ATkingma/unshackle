# TAAK-341 — xUnit v2 → v3 fix (`xunit3`)

**Status**: in progress (steps 1-5 done; next: corpus, step 7)
**Created**: 2026-10-27
**Branch / worktree**: `taak-341` (agent worktree `.claude/worktrees/agent-ad1cc69b2d7075655`)
**Goal**: An `xunit3` fix that migrates xUnit v2 test projects to xunit.v3: package renames, `OutputType Exe`, `Xunit.Abstractions`
usings, `async void` tests, `IAsyncLifetime` ValueTask, later `PropertyData`/typeof attribute args, Manual for extensibility and
dual-disposal classes; 100% on a corpus where every project builds after the fix.

## Background for a cold agent

- Scout report and council questions: `docs/projectinfo/architectuur/candidate-testfw.md` (TAAK-440). The user marked xunit3 a go.
- Template fix: `src/Unshackle.Core/Fixes/Polly/` (`IFix` + `IProjectFileMigration`, target-framework blocker).
- v3 keeps the `Xunit` namespace, so the engine's `PackageMigration` (removes the package once no code binds to the old assembly) does
  not fit: `[Fact]` always binds to v2 `xunit.core` while fixing. The package swap is therefore typed project-file edits in
  `XunitV3Fix.EditsFor` (`PackageRenameEdit`, `PackageRemoveEdit`, `PropertyEdit OutputType=Exe`).
- `AllOrNothing = true`: a Fixed rewrite (ValueTask, `using Xunit`) does not build on v2 and a Manual row leaves code that does not build
  on v3, so one Manual row anywhere leaves the whole solution unchanged.
- Test project detection: references `xunit.execution.dotnet`/`.desktop` (brought in by the `xunit`/`xunit.core` packages). Only those get
  `OutputType Exe` and `xunit`/`xunit.core` → `xunit.v3`; helper libraries only get the `xunit.v3.extensibility.core`/`xunit.v3.assert` renames.
- Proofs: "before" compiles against xUnit 2.9.3 copied to `bin/.../XunitV2/` (PackageReference with `IncludeAssets="none"`, see
  `src/Unshackle.Tests/Unshackle.Tests.csproj`), because our own test project runs on xunit.v3 and cannot compile against both.
  "After" uses the xunit.v3 4.0.2 assemblies of the test output (`src/Unshackle.Tests/Core/XunitV3/XunitV3References.cs`).
  All four v2 packages are Apache-2.0.

## Steps

- [x] 1. Fix skeleton `src/Unshackle.Core/Fixes/XunitV3/` (`XunitV3Fix`, `XunitV3Assemblies`, `XunitV3TargetFramework`, `XunitV3Reason`),
  registered in `src/Unshackle.Interface/Cli.cs`, `ProofTests`, `PreFilterContractTests`, `NewFixesSmokeTests`
- [x] 2. Project files: renames `xunit`/`xunit.core` → `xunit.v3`, `xunit.assert` → `xunit.v3.assert`, `xunit.extensibility.core|execution` →
  `xunit.v3.extensibility.core`, remove `xunit.abstractions`, `OutputType Exe` for test projects; blocker for net < 8 / netfx < 4.7.2 /
  netstandard test projects; Info row about `xunit.runner.visualstudio` ≥ 3.0
- [x] 3. Patterns with proofs + KingmaDoc: `xunit3.abstractions-namespace`, `xunit3.async-lifetime`, `xunit3.async-void-test`
- [x] 4. Unit tests `src/Unshackle.Tests/Core/XunitV3/`
- [x] 5a. X2: target xunit.v3 3.2.2 (latest 3.2.x on nuget.org, 2026-10-27; `XunitV3Fix.V3Version`). The proofs' "after" compiles against
  3.2.2 assemblies fetched by `PackageDownload` into the output folder `XunitV3/` (the test project itself stays on 4.0.2). 4.x opt-in: later
- [x] 5b. X4: test project = `xunit` package AND `Microsoft.NET.Test.Sdk` (seen as `Microsoft.TestPlatform.CoreUtilities`/`ObjectModel`/
  `CodeCoverage.Shim` references) AND an attribute deriving from `Xunit.FactAttribute`. Otherwise no `Exe`, `xunit`/`xunit.core` →
  `xunit.v3.extensibility.core` (+ `xunit.v3.assert` add when v2 assert was referenced), and an Info row naming which sign is missing
- [x] 5c. X6: `.AsTask()` on a direct use (initializer, assignment, return, argument); proof `as-task`; `result-used` now `.Wait()`/`.ContinueWith`
- [x] 5d. X1: `FixReport.BlockingProjects` (all-or-nothing revert) + `IConsoleView.ShowBlockingProjects` after the summary
- [x] 5e. X3: `PackageVersionEdit` (Name, From, To): bumps a plain version in [From, To) on the reference, a shared props file (G3 rules), or the
  central `PackageVersion` (only when every project under it that references the package is loaded and unblocked, else Info naming them);
  a non-plain version (`$(Prop)`, range) stays with an Info row. xunit3 test projects bump `xunit.runner.visualstudio` 2.0.0+ → 3.1.5 (step 6 runner part done)
- [x] 5f. X9: `PackageVersionEdit.BelowFromInfo`: a plain version below `From` is kept with that Info row; xunit3 sets it for the runner
  ("a 1.x runner cannot run xunit.v3 tests; update it to 3.1.5 by hand"). Unit tests `ApplyAsync_BelowFromWithInfo_KeptWithInfo`, `XunitV3FixTests` (branch `taak-341c`)
- [x] 5. Remaining patterns (branch `taak-341c`): `xunit3.record-exception-async` (not awaited → `.AsTask()`, method group Manual),
  `xunit3.type-name-attribute` (`[CollectionBehavior]`/`[TestCaseOrderer]`/`[TestCollectionOrderer]`/`[TestFramework]` (string, string) →
  `typeof(..)` for an own type, else Manual `TypeNameArgument`), `xunit3.extensibility` (Manual inventory: `Xunit.Sdk` bases/interfaces/
  discoverer attributes, `ITestCollectionOrderer`, `MemberDataAttributeBase`). Not built: `[PropertyData]` (council question 4 below),
  `TheoryData`/`SkipException` differences (question 5)
- [ ] 6. `xunit.runner.visualstudio` bump (needs a same-name version edit in the engine: `PackageRenameEdit` with OldName == NewName removes
  the reference, and under central package management the old `PackageVersion` stays), and `Microsoft.NET.Test.Sdk` minimum
- [ ] 7. Corpus (discovered-test count > 0 and equal before/after, X2): 10-15 xUnit 2.x repos incl. integration suites with `IAsyncLifetime` (scout survey gap), pinned shas, build after fix
- [ ] 8. Blind round, roadmap row in `docs/projectinfo/project.md`

## Council questions (decided in council round 18, `claude/tasks/COUNCIL-round-18.md`)

1. **AllOrNothing solution-wide vs. per project.** Built: solution-wide (one Manual row reverts everything). A per-project partial run
   (blocked project keeps v2 code and packages) would migrate more, but needs `AllowsPartial` rules and care for shared helper projects.
   **Decision (council 18 X1: solution-wide for now; summary lists blocking projects and reasons.)**
2. **Target version.** Built: xunit.v3 4.0.2 (what the proofs compile against; it pulls Microsoft Testing Platform v2 via
   `xunit.v3.mtp-v2`). 3.2.x (MTP v1) may be the safer default for VSTest-based CI. Which one?
   **Decision (council 18 X2: default 3.2.x (MTP v1); 4.x opt-in later after a VSTest check; corpus checks discovered-test count > 0 and equal.)**
3. **Runner package.** Built: Info row only. Should the engine get a same-name version-bump edit (also for central package management)
   so `xunit.runner.visualstudio` 2.x → 3.x is automatic?
   **Decision (council 18 X3: yes, new engine edit `PackageVersionEdit` (Name, From, To) incl. CPM; under CPM only when every user project is migrated, else Info naming the blockers.)**
4. **Test project detection** by the `xunit.execution.*` assembly: a helper library that references the full `xunit` package also becomes
   `Exe`. Acceptable (v3's `xunit.v3` package requires Exe anyway), or detect test projects by test attributes / `Microsoft.NET.Test.Sdk`?
   **Decision (council 18 X4: `Exe` + `xunit.v3` only with `Microsoft.NET.Test.Sdk` AND test attributes; either alone → Info, no `Exe`.)**
5. **`IAsyncLifetime` + `IDisposable`**: built as Manual `DualDisposal`. Could be Fixed by moving the `Dispose` body into `DisposeAsync`
   (with a warning), at the cost of a behaviour-sensitive rewrite.
   **Decision (council 18 X5: stays Manual.)**
6. **Non-awaited lifetime calls** (`Task t = f.InitializeAsync()`): built as Manual `LifetimeResultUsed`; `.AsTask()` would be a mechanical
   Fixed alternative.
   **Decision (council 18 X6: Fixed `.AsTask()` only when the result is used exactly once directly; else Manual.)**

## Council questions (from steps 5a-5e, decided in council round 20, `claude/tasks/COUNCIL-round-20.md`)

1. **Non-test project with the `xunit` package** (X4 "either alone"): built conservatively as `xunit`/`xunit.core` → `xunit.v3.extensibility.core`
   (+ `xunit.v3.assert`), OutputType unchanged, Info row. Keeping `xunit` v2 would mix v2 and v3 assemblies and not build. Acceptable?
   **Decision (council 20 X7: accepted as built.)**
2. **Test attribute detection** runs sync over the project's documents (`GetAwaiter().GetResult()`, as `IProjectFileMigration` is sync). Make
   `EditsFor`/`ProjectInfo` async later?
   **Decision (council 20 X8: yes, `IProjectFileMigration` becomes async with a required token; TAAK-352.)**
3. **Runner bump range**: `From` 2.0.0 (so an older 3.x below 3.1.5 is raised too). A 1.x runner is left alone (no row).
   **Decision (council 20 X9: `From` 2.0.0 accepted; a 1.x runner gets an Info row to update by hand; step 5f.)**

## Council questions (from step 5, open)

1. **`.AsTask()` on every non-awaited `Record.ExceptionAsync`.** Built: always Fixed (no "used exactly once" limit as in X6), because no
   declaration changes and `.AsTask()` returns exactly the v2 `Task<Exception>`. Acceptable?
2. **`typeof` rewrite next to a Manual orderer.** Built: an own type named by a string attribute becomes `typeof(..)`; the class itself is
   Manual via `xunit3.extensibility`, so under AllOrNothing the solution stays v2 until it is rewritten by hand. A built-in v2 type
   (`Xunit.Sdk.XunitTestFramework`) or another assembly's type stays Manual `TypeNameArgument` (no v2 → v3 name map). Map well-known built-ins?
3. **Extensibility inventory breadth.** Built: classes with a v2 `Xunit.Sdk` base type, own interface or attribute, plus `ITestCollectionOrderer`
   and `MemberDataAttributeBase`; `FactAttribute`/`TheoryData` subclasses are not reported. Also report uses (not declarations) of `Xunit.Sdk`
   types, e.g. `new ExceptionAggregator()`?
4. **`[PropertyData]`.** Not built: in xUnit 2.9.3 `Xunit.Extensions.PropertyDataAttribute` is `[Obsolete(error: true)]`, so a v2 project that
   builds cannot use it and a proof "before" cannot compile. Drop it from the scope?
5. **`TheoryData` / `SkipException`.** Not built: v2 has no `SkipException` (that is the `Xunit.SkippableFact` package, out of scope) and
   `TheoryData<T>` usage compiles on v3. Leave for the corpus to show a real break?

## Context for the next agent

- Done: steps 1-5 (5f and the step-5 patterns on branch `taak-341c`); all proofs and unit tests green, full `dotnet test` green, `verify-head.sh` VERIFY OK.
- Next: the open council questions (step 5), then step 7 (corpus) and step 8.
- Changed files: `src/Unshackle.Core/Fixes/XunitV3/**`, `src/Unshackle.Interface/Cli.cs`, `src/Unshackle.Tests/Core/XunitV3/**`,
  `src/Unshackle.Tests/Proofs/XunitV3/**`, `src/Unshackle.Tests/{ProofTests.cs,Core/Engine/PreFilterContractTests.cs,Core/EndToEnd/NewFixesSmokeTests.cs}`,
  `src/Unshackle.Tests/Unshackle.Tests.csproj`, `Directory.Packages.props`, `docs/projectinfo/patterns/xunit3-*`, `docs/projectinfo/README.md`.

## See also

- `docs/projectinfo/architectuur/candidate-testfw.md` — scout report, Fixed/Manual table, corpus survey
- `claude/tasks/TAAK-440-scout-testfw.md` — the scout task
- `docs/projectinfo/patterns/xunit3-async-lifetime-plan.md` — pattern plans (also `-abstractions-namespace`, `-async-void-test`)
- `claude/docs/council.md` — decision council
- `claude/tasks/COUNCIL-round-20.md` — X7-X9
- `claude/tasks/archief/TAAK-352-async-project-migration.md` — async project migrations
