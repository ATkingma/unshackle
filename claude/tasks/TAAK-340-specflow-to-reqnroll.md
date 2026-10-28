# TAAK-340 — SpecFlow → Reqnroll (fix)

**Status**: steps 4a-4c done (branch `taak-340c`); next: rest of step 4 (`<Using>` items), then step 5
**Created**: 2026-10-27
**Goal**: A `specflow` fix that migrates SpecFlow 3.x test projects to Reqnroll (packages, namespaces, renamed types, v3→v4 breaks, config), 100% on a SpecFlow corpus.

## Context for the next agent

- Why: SpecFlow is EOL since 2024-12-31 (GitHub projects deleted, last stable 3.9.74 from 2022). Reqnroll is its maintained BSD-3-Clause fork. Council round 8 S1 put EOL fixes in scope; S4 placed SpecFlow as roadmap step 11.
- Scout report with facts, API distance, 13-repo survey and the original council questions: `docs/projectinfo/architectuur/candidate-specflow.md` (TAAK-370).
- Route a new fix follows: `claude/tasks/TAAK-307-moq-to-nsubstitute.md`; code rules `claude/docs/code-standaarden.md`; pattern docs loop `claude/docs/patroon-loop.md`.
- Code: `src/Unshackle.Core/Fixes/SpecFlow/` (`SpecFlowFix`, `SpecFlowNames`, `Patterns/`). Registered in `src/Unshackle.Interface/Cli.cs`.
- Proof references: `SpecFlow` 3.9.74 and `Reqnroll` 3.3.4 in `Directory.Packages.props`, referenced by the test project with `IncludeAssets="compile;runtime"` (no generator build assets). Both BSD-3-Clause, no license or sponsor checks. `src/Unshackle.Tests/Core/SpecFlow/SpecFlowReferences.cs`.
- Proof harness change: `ProofFix.AfterRunsWholeFix` (true for SpecFlow). An `.after.cs` proof runs every pattern of the fix, because a rename pattern's output (CombinedTags, DataTableRow) only compiles together with the namespace swap; the folder's pattern must still report a finding. Manual proofs stay single-pattern.
- Design of the slice:
  - `SpecFlowFix : IFix, IProjectFileMigration`, **AllOrNothing** (a file on Reqnroll next to SpecFlow packages does not build). No `PackageMigration`: the package swap is a list of `PackageRenameEdit`s (`SpecFlow`, `.NUnit`, `.xUnit`, `.MsTest`, `.Tools.MsBuild.Generation` → `Reqnroll.*` 3.3.4), only for projects that reference `TechTalk.SpecFlow`/`BoDi`.
  - `ProjectBlockers`: a project whose SpecFlow assembly comes from a `packages/<Id>.<Version>/` folder (packages.config) is Manual (and so, AllOrNothing, the whole run).
  - `SpecFlowNames`: namespace swap `TechTalk.SpecFlow[.X]` → `Reqnroll[.X]`, `BoDi` → `Reqnroll.BoDi`; rename table (31 types: `*SpecFlow*` names, `TableRow(s)` → `DataTableRow(s)`, Assist extension classes moved to `Reqnroll`); removed types/namespaces (App.config, sync binding invoker). `SpecFlowNamesTests` checks the table against every public type of both real assemblies.
  - Patterns (run order; `NamespacePattern` last because the others need the SpecFlow usings to bind): `specflow.renamed-type`, `specflow.combined-tags`, `specflow.pending`, `specflow.string-step-call` (Manual), `specflow.async-void-binding` (Manual), `specflow.namespace`.
- Corpus repos (shallow clones from the scout): `<corpus>\corpus-candidates\specflow\src\<name>\`. Corpus builds use a NuGet cache on D: (`NUGET_PACKAGES`); never clear `~/.nuget/packages`.

## Steps

- [x] 1. Task file; packages SpecFlow 3.9.74 + Reqnroll 3.3.4 for proofs (license-clean)
- [x] 2. First slice: `SpecFlowFix` skeleton, package rename edits, packages.config blocker, `specflow.namespace`, `specflow.renamed-type`, `specflow.combined-tags`, `specflow.pending`, `specflow.string-step-call`, `specflow.async-void-binding`; proofs, unit tests, KingmaDoc docs, README rows
- [x] 3. Council round 18 (`claude/tasks/COUNCIL-round-18.md`, S1-S9); follow-ups:
  - [x] 3a. S2: run summary names every blocking project and its blocking rows
  - [x] 3b. S3/S4: reason texts (`async void` hand route; "possible match" binding hint for a single matching binding); S1: App.config / string-step rows name `Reqnroll.SpecFlowCompatibility`
  - [x] 3c. S7: drop a swapped using to which nothing in the file binds afterwards (proof)
  - [x] 3d. S9 (in step 4): `Directory.Build.props` package items are edited (row names the file); Manual blocker when the props file reaches a project outside the solution or not migrated, the item has a `Condition`, or the import/item cannot be evaluated statically
  - [x] 3e. S8 (in step 4): plugin without a port → project blocker naming the package; `SpecFlow.Autofac` → `Reqnroll.Autofac`
  - [x] 3f. Backlog task for per-project partial migration (unit: project + referenced binding projects)
- [x] 4. Project-level gaps: `<Using Include="TechTalk.SpecFlow" />` items; packages declared in `Directory.Build.props` (a `PackageRenameEdit` skips a project that does not declare the package, which would leave code on Reqnroll and packages on SpecFlow: add a blocker or an edit there; decided by council 18 S9, see 3d); plugin packages without a port (`SpecFlow.Plus.LivingDocPlugin`, `SpecFlow.Allure`, `SpecRun.*`, `SpecFlow.Assist.Dynamic`, `SpecFlow.Autofac` → `Reqnroll.Autofac`): detect from the project file and report or rename
  - [x] 4a. Council 21 S10: `SharedDeclarationBlocked` reason names the declaring file (line) and rename; ProjectReference-only reason names the declaring project
  - [x] 4b. Council 21 S11: license check, `SpecFlow.Autofac` + `Reqnroll.Autofac` in `Directory.Packages.props`, proof for namespace map and version pin (Manual "possible match" until then); unknown `SpecFlow.*`/`SpecRun.*` project-file references block
  - [x] 4c. Council 21 S12: plan lists the S7 exclusions literally; confirm the drop uses the semantic model
  - [x] 4d. `<Using Include="TechTalk.SpecFlow…" />` items: `UsingRenameEdit` per namespace (project file); one in `Directory.Build.props` is a `SharedDeclarationBlocked` Manual
- [ ] 5. Config (rename done; App.config section and `*.feature.cs` hint open): `specflow.json` → `reqnroll.json` (`ConfigFileRenameEdit` exists) plus key renames (`stepAssemblies` → `bindingAssemblies`, `bindingCulture/name` → `language/binding`) under council G5 (parsed-tree proof); App.config `<specFlow>` section Manual; checked-in `*.feature.cs` hint
- [x] 6. Step text (`specflow.step-expression`, Manual; corpus inventory still in step 7): Reqnroll detects Cucumber expressions; a regex step text without `^`/`$` can change meaning. Inventory the corpus, then Info or a rewrite (add `^...$`)
- [ ] 7. Corpus round (S8 UI-suite bar: build + equal discovered-test count; baseline build + `dotnet test` counts before/after; UI suites: build + discovered-test count only), blind round, roadmap row, README text (EOL, not license)

## Council questions

Decided in council round 18 (`claude/tasks/COUNCIL-round-18.md`). The `Directory.Build.props` risk of step 4 is S9 (edit with blockers, step 3d).

Open design choices; the slice takes the conservative option for each.

1. **Level 1 vs level 2** (scout Q2): the slice goes straight to level 2 (namespace rename, no `Reqnroll.SpecFlowCompatibility`). Is a compat-package mode wanted as a fallback for projects with App.config or string step calls?
   **Decision (council 18 S1: level 2 only; Manual rows name `Reqnroll.SpecFlowCompatibility`.)**
2. **AllOrNothing**: any Manual row (string step call, async void, packages.config, a removed type) leaves the whole solution on SpecFlow. A per-project partial migration (`IPartialFix`, code and packages moving per project) would migrate more. Wanted, and with what unit (project or solution)?
   **Decision (council 18 S2: AllOrNothing stays; summary names blocking projects and rows; partial migration on the backlog (unit: project + referenced binding projects).)**
3. **`async void` bindings**: Manual now. Rewrite to `async Task` (Reqnroll then waits for the work SpecFlow ran in the background) as Fixed with an Info row, or keep Manual?
   **Decision (council 18 S3: Manual; reason "Reqnroll awaits this; change to `async Task` by hand and check ordering".)**
4. **String step calls** (`Given("...")` on `Steps`): Manual now. Any rewrite (call the bound method, found by matching the step text against the bindings) acceptable, or always Manual?
   **Decision (council 18 S4: always Manual; a single matching binding is named as "possible match".)**
5. **`Pending()`**: rewritten to `throw new PendingStepException()` only as the last statement / expression body. Enough, or also elsewhere with the following statements removed?
   **Decision (council 18 S5: enough as built; elsewhere Manual with the line.)**
6. **Package rename list**: `SpecFlow.Tools.MsBuild.Generation` is renamed to `Reqnroll.Tools.MsBuild.Generation` (kept, though the runner packages bring it), the runner packages keep their role (`SpecFlow.xUnit` → `Reqnroll.xUnit`, xUnit v2). OK, or drop the generator reference?
   **Decision (council 18 S6: keep the generator rename.)**
7. **Unused new usings**: `using TechTalk.SpecFlow.Infrastructure;` becomes `using Reqnroll.Infrastructure;` even when, after a type moved to `Reqnroll` (`ISpecFlowOutputHelper`), nothing binds to it any more (IDE0005 in projects that enforce it). Remove such a using, or leave it?
   **Decision (council 18 S7: remove only a using our swap wrote that nothing binds to afterwards.)**
8. Scout questions still open: plugins without a port (Manual line or block the project, scout Q4); proof bar for UI suites (scout Q5); `packages.config` edit support (scout Q3: the slice blocks it).
   **Decision (council 18 S8: plugin without port → project blocker; with port → rename; packages.config blocked; UI suites: build + equal discovered-test count.)**

9. **S9 detection (step 3d)**: a package is "used" when its NuGet folder shows in a reference path (`<id>/<version>/lib/...`). Only
   the runner packages and `SpecFlow.Autofac` get `MustBeDeclared` (nothing else depends on them): when neither the project file nor its
   nearest `Directory.Build.props` / `Directory.Packages.props` declares them, the rename is a `SharedDeclarationBlocked` Manual. A runner
   package that reaches a project only through a `ProjectReference` is blocked too (conservative). An imported props file that declares it
   statically is not followed and edited (Manual instead). Follow static imports and edit there?
   **Decision (council 21 S10: no; the Manual row names the declaring file and the rename; ProjectReference-only names the declaring project.)**
10. **S8 detection**: unported plugins are seen only when they have an assembly in a reference path; a build-only package (possibly
    `SpecFlow.Plus.LivingDocPlugin`) is not seen until step 4 reads project files. `Reqnroll.Autofac` is pinned to `ReqnrollVersion`
    (Reqnroll ships its plugins with the same version) and the `SpecFlow.Autofac` namespace maps to `Reqnroll.Autofac` (not proven
    against the real plugin assemblies; no proof package). Add both packages to `Directory.Packages.props` for a proof?
    **Decision (council 21 S11: yes after a license check; Autofac rename Manual "possible match" until the proof passes; unknown `SpecFlow.*`/`SpecRun.*` references in step 4 block the project.)**
11. **S7 scope**: the dropped using must have only whitespace in front of it and must not be global, alias or static; files with a
    query expression or a deconstruction keep it (implicit binds not checked); the only using of its block is kept (a stray blank line otherwise, e.g. `RenamedType/output-helper`).
    **Decision (council 21 S12: accepted as built; exclusions listed in the plan; drop decided by the semantic model.)**

12. **Cucumber expressions (step 6)**: `specflow.step-expression` is Manual for a step text Reqnroll 3.3.4 reads as a Cucumber
    expression (its own `CucumberExpressionDetector`, copied) that holds one of `()[]{}.*+?|\/^$`. Under AllOrNothing this blocks
    any solution with such a text (e.g. a trailing `.`). A rewrite to `[Given("...", ExpressionType = ExpressionType.RegularExpression)]`
    keeps the SpecFlow regex meaning exactly (SpecFlow anchors regexes as Reqnroll does). Rewrite automatically (Fixed + Info), or stay Manual?
13. **specflow.json (step 5)**: Reqnroll 3.3.4 reads `reqnroll.json`, else `specflow.json` (`ReqnrollJsonLocator`), and still binds
    `stepAssemblies` and `bindingCulture` (`JsonConfig`, `JsonConfigurationLoader`; checked in the decompiled `Reqnroll.dll`). So the
    rename is the only edit: no key renames, the parsed tree is identical (G5 met trivially). The `Reqnroll.Tools.MsBuild.Generation`
    generator was not decompiled (not in the local NuGet cache); its config lookup is assumed to match. Rename at all (the old name works),
    or keep the file name and drop the edit?

## Progress log

- 2026-10-27: created from the scout report; first slice built on branch `taak-340` (6 patterns, 14 proofs, `SpecFlowFixTests`, `SpecFlowNamesTests`).
- 2026-10-27: steps 3a-3f on branch `taak-340b`: `PackageRenameEdit.MustBeDeclared` and the Condition blocker in `XmlProjectFileStore`
  (S9), unported-plugin blockers and the Autofac rename (S8), reason texts and "possible match" (S1, S3, S4), the dropped using (S7),
  blocking rows grouped per project in the summary (S2), backlog `TAAK-353` (3f). Step 4 keeps `<Using>` items and project-file plugin detection.
- 2026-10-27: steps 4a-4c on branch `taak-340c`. S10: the `MustBeDeclared` Manual names a static `<Import>` that declares the package
  (file, line, importer) or the ProjectReference project that does, plus the rename; a blocked shared file's row names the rename.
  S11: licence check clean (SpecFlow.Autofac 3.9.74 and Reqnroll.Autofac 3.3.4 both BSD-3-Clause, build files only copy the plugin
  dll); both in `Directory.Packages.props`. The proof found two gaps the map did not cover: sub-namespace
  `SpecFlow.Autofac.SpecFlowPlugin` → `Reqnroll.Autofac.ReqnrollPlugin`, and member `AddSpecFlowBindings` → `AddReqnrollBindings`
  (`SpecFlowNames.RenamedMembers`, done by `specflow.renamed-type`); `GetCreateScenarioContainerBuilder` has no twin (Manual). With
  those, the rename is proven and stays automatic. `IProjectProperties.PackageReferences` reads the project file and nearest
  `Directory.Build.props`: build-only unported plugins and any unknown `SpecFlow.*` package block the project. S12: exclusions listed
  in `specflow-namespace-plan.md` REQ-4; the drop is semantic. Remaining in step 4: `<Using Include="TechTalk.SpecFlow" />` items.

- 2026-10-28: branch `taak-340e`. `UsingRenameEdit` (new typed edit, `XmlProjectFileStore.RenameUsingAsync`) for `<Using>` items;
  `SpecFlowFix` emits it per namespace plus `ConfigFileRenameEdit` specflow.json → reqnroll.json (no key renames, see question 13);
  `specflow.step-expression` pattern (Manual, question 12) with proof `StepExpression/optional-text` and KingmaDoc docs.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Fixes/SpecFlow/`, `src/Unshackle.Interface/Cli.cs`, `src/Unshackle.Tests/{Core/SpecFlow,Proofs/SpecFlow,ProofTests.cs,ProofFix.cs}`, `Directory.Packages.props`, `docs/projectinfo/patterns/specflow-*`, `docs/projectinfo/README.md` |
| **How**   | New fix (Strategy) with an `IProjectFileMigration`; patterns per API shape; name map verified against both real assemblies |
| **Why**   | SpecFlow is abandoned; Reqnroll is the maintained drop-in fork |

## See also

- `docs/projectinfo/architectuur/candidate-specflow.md` — scout report and survey
- `claude/tasks/TAAK-370-scout-specflow.md` — the scout task
- `claude/tasks/TAAK-353-specflow-partial-migration.md` — backlog: per-project partial migration (S2)
- `claude/docs/council.md` — rounds 8 (S1, S3, S4) and 10 (G4, G5)
- `docs/projectinfo/README.md` — pattern table
