---
kingmadoc: 1
feature: "specflow-pending"
status: approved
requirements: [REQ-1, REQ-2]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/PendingPattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/Pending/
---

# Feature: specflow.pending

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-340 (SpecFlow → Reqnroll, first slice) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.pending: `scenarioContext.Pending();` (the step skeleton SpecFlow generates) and `ScenarioContext.StepIsPending()` become `throw new PendingStepException();`; Reqnroll marks both obsolete, which breaks a build with warnings as errors.

## Scope (in / out)

**In scope**: The call as the last statement of a block, or as the body of an expression-bodied method or local function, on a receiver without side effects (a local, a field, `this.x`, `ScenarioContext.Current`).

**Out of scope**: Any other position (Manual: a throw would make the rest unreachable); receivers with side effects (Manual).

## Requirements

- **REQ-1**: WHEN Pending() is the last statement or an expression body THE SYSTEM SHALL throw PendingStepException instead (SpecFlow 3.9 Pending() does exactly that) (proof/test: skeleton).
- **REQ-2**: WHEN statements follow it THE SYSTEM SHALL report Manual (proof/test: not-last).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-pending-modellen.md](specflow-pending-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
