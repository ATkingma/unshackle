---
kingmadoc: 1
feature: "specflow-renamed-type"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/RenamedTypePattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/RenamedType/
---

# Feature: specflow.renamed-type

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-340 (SpecFlow → Reqnroll, first slice) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.renamed-type: A SpecFlow type that Reqnroll renamed or moved (`ISpecFlowOutputHelper` → `IReqnrollOutputHelper`, `SpecFlowContext` → `ReqnrollContext`, `TableRow` → `DataTableRow`, `Assist.TableHelpers` → `Reqnroll.TableHelpers`) is renamed where it is named by its simple name; the new namespace is imported.

## Scope (in / out)

**In scope**: Every entry of `SpecFlowNames.RenamedTypes` (checked against both real assemblies by `SpecFlowNamesTests`); constructors (`new SpecFlowOutputHelper(...)`); types without a Reqnroll twin (`SpecFlowNames.RemovedTypes`) are Manual.

**Out of scope**: Names qualified by a namespace (`specflow.namespace`); `var` (nothing to rename).

## Requirements

- **REQ-1**: WHEN a simple name binds to a renamed SpecFlow type THE SYSTEM SHALL write the Reqnroll name and import its namespace (proof/test: output-helper, table-row).
- **REQ-2**: WHEN it binds to a type without a Reqnroll twin THE SYSTEM SHALL report Manual (proof/test: removed-type).
- **REQ-3**: THE SYSTEM SHALL only map to types that exist in Reqnroll 3.3.4 (proof/test: SpecFlowNamesTests).
- **REQ-4** (council 21 S11): WHEN a simple name binds to a member Reqnroll renamed on a type that keeps its name (`SpecFlowNames.RenamedMembers`: `AddSpecFlowBindings` → `AddReqnrollBindings` of the Autofac plugin) THE SYSTEM SHALL write the new member name (proof/test: autofac-bindings).
- **REQ-5** (council 21 S11): WHEN it binds to a member without a Reqnroll twin (`SpecFlowNames.RemovedMembers`: the Autofac finder's `GetCreateScenarioContainerBuilder`) THE SYSTEM SHALL report Manual; every other public plugin member has a same-name twin (proof/test: autofac-finder, SpecFlowNamesTests.EveryPublicAutofacPluginMember_HasATwin_OrIsListed).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-renamed-type-modellen.md](specflow-renamed-type-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
