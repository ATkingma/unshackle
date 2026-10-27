# Council round 18 — SpecFlow → Reqnroll, xUnit v3, EPPlus flow follow-ups

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint
(round 1 split on S2, S3, S4, S7, S8, S9, X1, X3, X4, X6, E29, E31; round 2 accepted the proposals below; the round-2 amendments were
additive and are folded in).
Questions: council questions 1-8 plus the `Directory.Build.props` risk of step 4 in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`,
council questions 1-6 in `claude/tasks/TAAK-341-xunit-v3.md`, council questions 1-6 in `claude/tasks/archief/TAAK-336-epplus-flow-followups.md`.
Follow-ups: TAAK-340 steps 3a-3f, TAAK-341 steps 5a-5e, TAAK-346 (E27, E29, E31, E32). TAAK-347 is not used.

| # | Decision | Joint rule |
| --- | --- | --- |
| S1 | Level 1 (compat package) vs level 2 (TAAK-340 Q1) | Level 2 only, no compat mode. App.config and string-step-call Manual rows name `Reqnroll.SpecFlowCompatibility` as the hand route. |
| S2 | AllOrNothing (Q2) | Keep solution-wide AllOrNothing for now. The run summary names every blocking project and its blocking rows. Partial migration goes on the backlog; its unit is a project plus the binding projects it references. |
| S3 | `async void` bindings (Q3) | Manual. The reason says "Reqnroll awaits this; change to `async Task` by hand and check ordering". |
| S4 | String step calls (Q4) | Always Manual. When exactly one binding matches, the row names it as a "possible match" (hint, no rewrite). |
| S5 | `Pending()` (Q5) | Last statement / expression body only, as built; elsewhere Manual with the line. |
| S6 | Generator package (Q6) | Keep renaming `SpecFlow.Tools.MsBuild.Generation` → `Reqnroll.Tools.MsBuild.Generation` (one rule: every `SpecFlow.*` with a twin is renamed). |
| S7 | Unused new usings (Q7) | Remove only a using that our namespace swap wrote and to which nothing in the file binds afterwards; every other using is untouched. |
| S8 | Plugins, UI suites, packages.config (Q8) | Plugin without a port → project blocker (Manual naming the package); plugin with a port (`SpecFlow.Autofac` → `Reqnroll.Autofac`) → rename. `packages.config` stays blocked. UI-suite proof bar: build plus discovered-test count equal before/after. |
| S9 | Packages declared in `Directory.Build.props` (step 4) | Edit, not a blanket blocker: the edit goes in the props file and the row names that file. Manual blocker when the props file is imported by a project outside the analysed solution or not being migrated, when the package item has a `Condition`, or when the import chain or item cannot be evaluated statically (conditional or wildcard import, property-driven names). |
| X1 | AllOrNothing (TAAK-341 Q1) | Solution-wide for now; the summary lists the blocking projects and their reasons (as S2). |
| X2 | Target version (Q2) | Default xunit.v3 3.2.x (MTP v1; VSTest CI keeps working through `xunit.runner.visualstudio` 3.x). 4.x (MTP v2 via `xunit.v3.mtp-v2`) becomes opt-in later, after checking its readme/release notes for VSTest. Our own `global.json` (`test.runner = Microsoft.Testing.Platform`) hides the difference, so the corpus step checks a discovered-test count > 0 and equal before/after. |
| X3 | Runner package bump (Q3) | Yes, as a new engine edit `PackageVersionEdit` (Name, From, To), not `PackageRenameEdit` with equal names; it covers central package management. Under CPM the bump only happens when every project using that `PackageVersion` is migrated; otherwise an Info row names the blocking projects. |
| X4 | Test project detection (Q4) | `OutputType Exe` and the `xunit.v3` rename only when the project references `Microsoft.NET.Test.Sdk` AND contains test attributes; either alone → no `Exe`, Info row. |
| X5 | `IAsyncLifetime` + `IDisposable` (Q5) | Manual `DualDisposal` stays. |
| X6 | Non-awaited lifetime calls (Q6) | Fixed with `.AsTask()` only when the call result is used exactly once, directly (assigned, returned or passed); any other use stays Manual `LifetimeResultUsed`. |
| E27 | Field after proof (TAAK-336 Q1) | Accepted: `_Combined/field` (run by `ProofTests` in CI) proves the Fixed field; the plan names that proof. |
| E28 | E22 index/lookup (Q2) | Accepted: stays `FlowReturnNotMapped` (EPPlus null vs ClosedXML throw). |
| E29 | Names added by a return helper (Q3) | Lookups stay Manual; a rename in the caller to a name a return helper adds also becomes Manual, with a test. |
| E30 | E21 field uses (Q4) | Accepted as built; widen only on corpus evidence. |
| E31 | E24 and E6 (Q5) | One row per node: when E6 applies, the flow-types reason is folded into the E6 row text; the summary counts the node once. |
| E32 | E23 depth (Q6) | Accepted; the Manual row of a linked set names the failing member. |

## S1-S9 — SpecFlow → Reqnroll

- G: run-time binding discovery is per assembly; a half-migrated solution can end with undefined steps. `async void` → `async Task` and
  string-step rewrites change timing or binding choice silently. Editing a shared props file can change projects never analysed.
- P: AllOrNothing is the biggest source of hand work; accepted for now when the summary names the blockers and partial migration is on the
  backlog. A props edit beats blocking every project of a larger solution.
- A: two modes per fix are two rules; one rename rule for `SpecFlow.*`; `IProjectFileStore` already handles `Directory.Build.props`, so a
  blanket blocker would refuse work the engine can do; unclear imports must not be guessed.

## X1-X6 — xUnit v3

- G: 4.x on VSTest CI may run green with zero tests, the worst silent failure; a shared CPM version must not be bumped for unmigrated projects.
- P: a migration that breaks CI is the worst result; the runner bump removes a hand edit per repo.
- A: a version bump is its own edit type with an honest `Describe()`; "AND" detection keeps a helper library's output type predictable.
  P's `.AsTask()` and G's Manual met at "exactly one direct use" (the ValueTask single-consumption rule holds).

## E27-E32 — EPPlus flow follow-ups

- G: a rename to a helper-added name throws only at run time in ClosedXML → Manual.
- P: Manual rows must say where to start (the failing member).
- A: one row per node keeps the user's count right; no silent check gaps.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-17.md`
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md`
- `claude/tasks/TAAK-341-xunit-v3.md`
- `claude/tasks/archief/TAAK-346-epplus-flow-rows.md`
