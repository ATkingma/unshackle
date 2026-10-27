---
kingmadoc: 1
feature: "specflow-combined-tags"
status: approved
requirements: [REQ-1]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/CombinedTagsPattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/CombinedTags/
---

# Feature: specflow.combined-tags

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-340 (SpecFlow → Reqnroll, first slice) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.combined-tags: `ScenarioInfo.ScenarioAndFeatureTags` becomes `ScenarioInfo.CombinedTags` (renamed in SpecFlow v4, which Reqnroll is based on).

## Scope (in / out)

**In scope**: Every property access binding to `TechTalk.SpecFlow.ScenarioInfo.ScenarioAndFeatureTags`.

**Out of scope**: Reflection or strings naming the property.

## Requirements

- **REQ-1**: WHEN code reads ScenarioInfo.ScenarioAndFeatureTags THE SYSTEM SHALL read CombinedTags instead (same value: scenario tags plus feature tags) (proof/test: tags).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-combined-tags-modellen.md](specflow-combined-tags-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
