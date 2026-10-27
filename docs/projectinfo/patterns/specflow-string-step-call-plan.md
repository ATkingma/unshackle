---
kingmadoc: 1
feature: "specflow-string-step-call"
status: approved
requirements: [REQ-1]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/StringStepCallPattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/StringStepCall/
---

# Feature: specflow.string-step-call

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-340 (SpecFlow → Reqnroll, first slice) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.string-step-call: `Given("...")`, `When`, `Then`, `And`, `But` called on a `Steps` base class were removed in SpecFlow v4 and so in Reqnroll: always Manual.

## Scope (in / out)

**In scope**: Every call binding to `TechTalk.SpecFlow.Steps.Given/When/Then/And/But`.

**Out of scope**: Rewriting them (calling the step method or a driver is a design choice; council question in TAAK-340).

## Requirements

- **REQ-1**: WHEN code calls a string step method of Steps THE SYSTEM SHALL report Manual, and the fix (AllOrNothing) leaves the solution on SpecFlow (proof/test: steps-base, SpecFlowFixTests.FixAsync_StringStepCallInOneFile_LeavesTheWholeSolutionOnSpecFlow).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-string-step-call-modellen.md](specflow-string-step-call-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
