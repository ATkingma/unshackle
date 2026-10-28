---
kingmadoc: 1
feature: "specflow-step-expression"
status: approved
requirements: [REQ-1]
files_expected:
    - src/Unshackle.Core/Fixes/SpecFlow/Patterns/StepExpressionPattern.cs
    - src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs
    - src/Unshackle.Tests/Proofs/SpecFlow/StepExpression/
---

# Feature: specflow.step-expression

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-28 by KingmaDoc |

> Drafted from TAAK-340 step 6 (SpecFlow → Reqnroll, Cucumber expressions) and the scout report `docs/projectinfo/architectuur/candidate-specflow.md`.

## One-sentence summary

specflow.step-expression: A step text that SpecFlow reads as a regex but Reqnroll 3.3.4 reads as a Cucumber expression, and that holds a character whose meaning differs, is Manual.

## Scope (in / out)

**In scope**: The first positional string of an attribute deriving from `TechTalk.SpecFlow.StepDefinitionBaseAttribute`, judged by a copy of Reqnroll 3.3.4 `CucumberExpressionDetector.IsCucumberExpression` (checked in the decompiled `Reqnroll.dll`: `^`/`$` anchor → regex; `{\w*}` → Cucumber; `(...*)`/`(...+)`/`.*`/`\.`/`\d+` → regex; else Cucumber), and holding one of `()[]{}.*+?|\/^$`. Plain text matches the same under both and is left alone.

**Out of scope**: Rewriting (adding `ExpressionType = ExpressionType.RegularExpression`, or `^...$`): council question 12 in TAAK-340. Step texts built from non-constant values (attributes only take constants).

## Requirements

- **REQ-1**: WHEN a step text is read as a Cucumber expression by Reqnroll and holds a regex or Cucumber special character THE SYSTEM SHALL report Manual (proof: optional-text; test: `StepExpression_ChangesMeaning_*`).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-step-expression-modellen.md](specflow-step-expression-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
