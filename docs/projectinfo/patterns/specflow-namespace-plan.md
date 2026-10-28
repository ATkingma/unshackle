---
kingmadoc: 1
feature: "specflow-namespace"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/NamespacePattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/Namespace/
---

# Feature: specflow.namespace

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-340 (SpecFlow → Reqnroll, first slice) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.namespace: `using TechTalk.SpecFlow[.X];` becomes `using Reqnroll[.X];`, `using BoDi;` becomes `using Reqnroll.BoDi;`, and names written out in full (`TechTalk.SpecFlow.Table`, `global::TechTalk.SpecFlow.ScenarioContext`) get the same swap.

## Scope (in / out)

**In scope**: Plain, global, alias and static using directives; qualified names and member accesses in code (also `global::`); a fully written renamed type (rewritten as a whole to its new full name); `using TechTalk.SpecFlow.Assist;` also asks for `using Reqnroll` (the Assist extension methods moved there).

**Out of scope**: `<Using Include="TechTalk.SpecFlow" />` items in the project file (open step in TAAK-340); generated `*.feature.cs`.

## Requirements

- **REQ-1**: WHEN a using directive or a written-out name binds to a SpecFlow or BoDi namespace THE SYSTEM SHALL swap it to the Reqnroll namespace (proof/test: steps, assist, bodi, qualified).
- **REQ-2**: WHEN the namespace has no Reqnroll twin (TechTalk.SpecFlow.Configuration.AppConfig) THE SYSTEM SHALL report Manual (proof/test: app-config).
- **REQ-3**: THE SYSTEM SHALL run this pattern last, so the patterns before it still bind the SpecFlow types (proof/test: SpecFlowFixTests.Patterns_IdsUseSpecFlowPrefix_NamespacePatternLast).
- **REQ-4** (council 18 S7, council 21 S12): WHEN nothing in the file binds through a swapped namespace using THE SYSTEM SHALL drop that using instead of swapping it. The drop is decided by the semantic model: every simple name, `foreach` enumerator, `await` awaiter and collection-initializer `Add` in the file is bound, and the using goes only when none of those symbols lives in the swapped namespace after the swap; position alone never decides. Exclusions (the using is kept, swapped): (a) a `global` using; (b) an alias using; (c) a `static` using; (d) a using with anything but whitespace in front of it (a comment, a directive); (e) a file with a query expression, a deconstruction (`(a, b) = ...`, `var (a, b) = ...`) or a `foreach` with deconstruction, whose implicit binds are not checked; (f) the only using of its block (compilation unit or namespace), because removing it leaves a stray blank line (`RenamedType/output-helper`). No Info row for a kept using (proof/test: Namespace/assist drops `using Reqnroll.Assist;`; RenamedType/output-helper keeps the only using).
- **REQ-5** (council 21 S11): THE SYSTEM SHALL map `SpecFlow.Autofac` to `Reqnroll.Autofac` and `SpecFlow.Autofac.SpecFlowPlugin` to `Reqnroll.Autofac.ReqnrollPlugin`, proved against SpecFlow.Autofac 3.9.74 and Reqnroll.Autofac 3.3.4 (proof/test: SpecFlowNamesTests.EveryPublicAutofacPluginType_HasATwinUnderTheSwappedNamespace, RenamedType/autofac-bindings).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing per unit (TAAK-353, council 18 S2): a Manual row keeps its project, every SpecFlow project linked to it
  by a `ProjectReference` (either way, also through other projects) and every project sharing a file with it on SpecFlow (`specflow.kept`
  row, `KeptReason.SpecFlowProjectKept`); the other units move. A blocking row outside every project, or every SpecFlow project kept,
  leaves the solution on SpecFlow.
- Project blockers (TAAK-340 step 5): a `<specFlow>` section in the project's App.config is Manual (Reqnroll 3.3.4's
  `ConfigurationLoader.GetAppConfigStub` returns the defaults, so the settings would be lost); checked-in `*.feature.cs` code-behind is an
  Info row (the Reqnroll generator rewrites it next to the `.feature` file on build, `ReqnrollUseIntermediateOutputPathForCodeBehind` false).

## Models

[specflow-namespace-modellen.md](specflow-namespace-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
