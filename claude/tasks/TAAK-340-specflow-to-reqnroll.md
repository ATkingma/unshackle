# TAAK-340 — SpecFlow → Reqnroll (fix)

**Status**: step 2/7 — first slice done (skeleton, package rename, namespace swap, five code patterns), branch `taak-340`
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
- [ ] 3. Council round on the questions below (at least 1, 2, 6) before widening the scope
- [ ] 4. Project-level gaps: `<Using Include="TechTalk.SpecFlow" />` items; packages declared in `Directory.Build.props` (a `PackageRenameEdit` skips a project that does not declare the package, which would leave code on Reqnroll and packages on SpecFlow: add a blocker or an edit there); plugin packages without a port (`SpecFlow.Plus.LivingDocPlugin`, `SpecFlow.Allure`, `SpecRun.*`, `SpecFlow.Assist.Dynamic`, `SpecFlow.Autofac` → `Reqnroll.Autofac`): detect from the project file and report or rename
- [ ] 5. Config: `specflow.json` → `reqnroll.json` (`ConfigFileRenameEdit` exists) plus key renames (`stepAssemblies` → `bindingAssemblies`, `bindingCulture/name` → `language/binding`) under council G5 (parsed-tree proof); App.config `<specFlow>` section Manual; checked-in `*.feature.cs` hint
- [ ] 6. Step text: Reqnroll detects Cucumber expressions; a regex step text without `^`/`$` can change meaning. Inventory the corpus, then Info or a rewrite (add `^...$`)
- [ ] 7. Corpus round (baseline build + `dotnet test` counts before/after; UI suites: build + discovered-test count only), blind round, roadmap row, README text (EOL, not license)

## Council questions

Open design choices; the slice takes the conservative option for each.

1. **Level 1 vs level 2** (scout Q2): the slice goes straight to level 2 (namespace rename, no `Reqnroll.SpecFlowCompatibility`). Is a compat-package mode wanted as a fallback for projects with App.config or string step calls?
2. **AllOrNothing**: any Manual row (string step call, async void, packages.config, a removed type) leaves the whole solution on SpecFlow. A per-project partial migration (`IPartialFix`, code and packages moving per project) would migrate more. Wanted, and with what unit (project or solution)?
3. **`async void` bindings**: Manual now. Rewrite to `async Task` (Reqnroll then waits for the work SpecFlow ran in the background) as Fixed with an Info row, or keep Manual?
4. **String step calls** (`Given("...")` on `Steps`): Manual now. Any rewrite (call the bound method, found by matching the step text against the bindings) acceptable, or always Manual?
5. **`Pending()`**: rewritten to `throw new PendingStepException()` only as the last statement / expression body. Enough, or also elsewhere with the following statements removed?
6. **Package rename list**: `SpecFlow.Tools.MsBuild.Generation` is renamed to `Reqnroll.Tools.MsBuild.Generation` (kept, though the runner packages bring it), the runner packages keep their role (`SpecFlow.xUnit` → `Reqnroll.xUnit`, xUnit v2). OK, or drop the generator reference?
7. **Unused new usings**: `using TechTalk.SpecFlow.Infrastructure;` becomes `using Reqnroll.Infrastructure;` even when, after a type moved to `Reqnroll` (`ISpecFlowOutputHelper`), nothing binds to it any more (IDE0005 in projects that enforce it). Remove such a using, or leave it?
8. Scout questions still open: plugins without a port (Manual line or block the project, scout Q4); proof bar for UI suites (scout Q5); `packages.config` edit support (scout Q3: the slice blocks it).

## Progress log

- 2026-10-27: created from the scout report; first slice built on branch `taak-340` (6 patterns, 14 proofs, `SpecFlowFixTests`, `SpecFlowNamesTests`).

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Fixes/SpecFlow/`, `src/Unshackle.Interface/Cli.cs`, `src/Unshackle.Tests/{Core/SpecFlow,Proofs/SpecFlow,ProofTests.cs,ProofFix.cs}`, `Directory.Packages.props`, `docs/projectinfo/patterns/specflow-*`, `docs/projectinfo/README.md` |
| **How**   | New fix (Strategy) with an `IProjectFileMigration`; patterns per API shape; name map verified against both real assemblies |
| **Why**   | SpecFlow is abandoned; Reqnroll is the maintained drop-in fork |

## See also

- `docs/projectinfo/architectuur/candidate-specflow.md` — scout report and survey
- `claude/tasks/TAAK-370-scout-specflow.md` — the scout task
- `claude/docs/council.md` — rounds 8 (S1, S3, S4) and 10 (G4, G5)
- `docs/projectinfo/README.md` — pattern table
