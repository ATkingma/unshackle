# TAAK-341 — xUnit v2 → v3 fix (`xunit3`)

**Status**: in progress (step 1-4 done, first slice on branch `taak-341`)
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
- [ ] 5. Remaining patterns: `Record.ExceptionAsync` (ValueTask), `[PropertyData]` → `[MemberData]`, string-based `[CollectionBehavior]`/
  `[TestCaseOrderer]`/`[TestFrameworkAttribute]` → `typeof(...)`, Manual inventory of extensibility (custom `DataAttribute`, discoverers,
  `Xunit.Sdk` runners), `TheoryData` / `SkipException` differences
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

## Council questions (open, from steps 5a-5e)

1. **Non-test project with the `xunit` package** (X4 "either alone"): built conservatively as `xunit`/`xunit.core` → `xunit.v3.extensibility.core`
   (+ `xunit.v3.assert`), OutputType unchanged, Info row. Keeping `xunit` v2 would mix v2 and v3 assemblies and not build. Acceptable?
2. **Test attribute detection** runs sync over the project's documents (`GetAwaiter().GetResult()`, as `IProjectFileMigration` is sync). Make
   `EditsFor`/`ProjectInfo` async later?
3. **Runner bump range**: `From` 2.0.0 (so an older 3.x below 3.1.5 is raised too). A 1.x runner is left alone (no row).

## Context for the next agent

- Done: steps 1-4; all xunit3 proofs and unit tests green, full `dotnet test` green, `verify-head.sh` VERIFY OK on the branch head.
- Next: step 5 (one pattern per sub-step, same proof/doc loop), or the council questions above first.
- Changed files: `src/Unshackle.Core/Fixes/XunitV3/**`, `src/Unshackle.Interface/Cli.cs`, `src/Unshackle.Tests/Core/XunitV3/**`,
  `src/Unshackle.Tests/Proofs/XunitV3/**`, `src/Unshackle.Tests/{ProofTests.cs,Core/Engine/PreFilterContractTests.cs,Core/EndToEnd/NewFixesSmokeTests.cs}`,
  `src/Unshackle.Tests/Unshackle.Tests.csproj`, `Directory.Packages.props`, `docs/projectinfo/patterns/xunit3-*`, `docs/projectinfo/README.md`.

## See also

- `docs/projectinfo/architectuur/candidate-testfw.md` — scout report, Fixed/Manual table, corpus survey
- `claude/tasks/TAAK-440-scout-testfw.md` — the scout task
- `docs/projectinfo/patterns/xunit3-async-lifetime-plan.md` — pattern plans (also `-abstractions-namespace`, `-async-void-test`)
- `claude/docs/council.md` — decision council
