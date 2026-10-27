---
kingmadoc: 1
feature: "specflow-async-void-binding"
status: approved
requirements: [REQ-1]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/AsyncVoidBindingPattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/AsyncVoidBinding/
---

# Feature: specflow.async-void-binding

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-340 (SpecFlow → Reqnroll, first slice) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.async-void-binding: An `async void` step definition or hook fails in Reqnroll: Manual, because making it `async Task` changes behaviour (Reqnroll then waits for it, SpecFlow did not).

## Scope (in / out)

**In scope**: Methods with `async` and `void` that carry an attribute deriving from `TechTalk.SpecFlow.StepDefinitionBaseAttribute` or `HookAttribute`.

**Out of scope**: Rewriting to `async Task` (council question in TAAK-340).

## Requirements

- **REQ-1**: WHEN a binding method is async void THE SYSTEM SHALL report Manual (proof/test: async-void).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-async-void-binding-modellen.md](specflow-async-void-binding-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
