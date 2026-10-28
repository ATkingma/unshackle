---
kingmadoc: 1
feature: "specflow-step-expression"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
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

specflow.step-expression: A step text that SpecFlow reads as a regex but Reqnroll 3.3.4 reads as a Cucumber expression, and that holds a character whose meaning differs, gets `ExpressionType = ExpressionType.RegularExpression` (Fixed + Info, council 23 S13); an escaped trailing `$` and an interpolated or unreadable text are Manual.

## Scope (in / out)

**In scope**: The first positional string of an attribute deriving from `TechTalk.SpecFlow.StepDefinitionBaseAttribute`, judged by a copy of Reqnroll 3.3.4 `CucumberExpressionDetector.IsCucumberExpression` (checked in the decompiled `Reqnroll.dll`: `^`/`$` anchor → regex; `{\w*}` → Cucumber; `(...*)`/`(...+)`/`.*`/`\.`/`\d+` → regex; else Cucumber), and holding one of `()[]{}.*+?|\/^$`. Plain text matches the same under both and is left alone.

The pin is added only on `Given`/`When`/`Then`/`StepDefinition` whose first argument is a string literal or a constant with a compile-time value and that set no `ExpressionType` yet; a qualified attribute name gets `global::Reqnroll.ExpressionType`. A text ending in an escaped `$` (odd backslash run before the final `$`, counted on the cooked value) is Manual whatever the detector says: SpecFlow anchors its end, Reqnroll does not.

A project that references the `CucumberExpressions.SpecFlow.3-x` plugin (assembly `CucumberExpressions.SpecFlow.SpecFlowPlugin`, TAAK-377, corpus shouty.net) already reads step texts as Cucumber expressions under SpecFlow; its detector (checked in the 1.0.6 dll) is Reqnroll's without the `\.`/`\d+` rule. There nothing is pinned; only a text that the plugin reads as Cucumber and Reqnroll as regex is Manual (an escaped trailing `$` stays Manual).

**Out of scope**: Adding `^...$` to texts. Custom step attributes (Manual).

## Requirements

- **REQ-1**: WHEN a literal or constant step text of `Given`/`When`/`Then`/`StepDefinition` without `ExpressionType` is read as a Cucumber expression by Reqnroll and holds a regex or Cucumber special character THE SYSTEM SHALL add `ExpressionType = ExpressionType.RegularExpression` with an Info "kept as regex via ExpressionType.RegularExpression" (proofs: literal, const; test: `StepExpression_ChangesMeaning_*`).
- **REQ-2**: WHEN a step text ends in an escaped `$` THE SYSTEM SHALL report Manual (proof: escaped-dollar; test: `StepExpression_EndsWithEscapedDollar_*`).
- **REQ-3**: WHEN a step text is interpolated or has no compile-time value THE SYSTEM SHALL report Manual (proof: interpolated).
- **REQ-4**: WHEN the attribute already sets `ExpressionType` or is a custom step attribute THE SYSTEM SHALL report Manual (no proof: SpecFlow 3.9.74 has no `ExpressionType`, so such a before file cannot compile).

- **REQ-5**: WHEN the compilation references `CucumberExpressions.SpecFlow.SpecFlowPlugin` THE SYSTEM SHALL leave step texts unchanged, and report Manual only for a text the plugin reads as Cucumber and Reqnroll as regex (proofs: plugin-differs, and `Namespace/cucumber-plugin-step-text` where the whole fix leaves the text alone, as the pattern finds nothing; test: `StepExpression_PluginDiffers_*`).

## Assumptions

- SpecFlow 3.9.74 is the old library, Reqnroll 3.3.4 the new one (both BSD-3-Clause, `Directory.Packages.props`).
- `SpecFlowFix` is AllOrNothing: one Manual row anywhere leaves the solution on SpecFlow.

## Models

[specflow-step-expression-modellen.md](specflow-step-expression-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
