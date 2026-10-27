---
kingmadoc: 1
feature: "specflow-namespace"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
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

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-namespace-modellen.md](specflow-namespace-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
