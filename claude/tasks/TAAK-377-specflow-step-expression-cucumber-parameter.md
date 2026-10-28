# TAAK-377 — specflow.step-expression pins a `{int}` step text as regex (test regression)

**Status**: open
**Created**: 2026-10-28
**Goal**: A step text that SpecFlow 3.x already matched as a Cucumber expression (`{int}`, `{string}`, ... parameter types) keeps its meaning after the fix.

## Context for the next agent

- Found in the SpecFlow corpus round 1 (`claude/tasks/CORPUSRUN-specflow.md`), repo `cucumber-examples/shouty.net` (SpecFlow 3.7.38).
- `[Given(@"Lucy is at {int}, {int}")]` passed on SpecFlow 3.7.38. The fix rewrote it to
  `[Given("...", ExpressionType = ExpressionType.RegularExpression)]` (Fixed + Info, council 23 S13). Reqnroll then reads `{int}` as a
  literal regex and the step is undefined: `InRangeShoutIsHeard` went Passed → NotExecuted ("No matching step definition found").
- So the S13 premise "SpecFlow reads every step text as a regex" is wrong for SpecFlow 3.x texts with Cucumber parameter types; it
  holds only for texts whose special characters are regex syntax (`(s)`, `.*`, ...).
- Pending proof: `src/Unshackle.Tests/Proofs/SpecFlow/_Pending/StepExpression/cucumber-parameter.{before,after}.cs` (after: namespace swap
  only, no pin). Move it to `Proofs/SpecFlow/StepExpression/` when fixed.

## Steps

- [ ] 1. Check from which SpecFlow 3.x version Cucumber expressions are recognised (decompile `TechTalk.SpecFlow` 3.7.38 / 3.9.74 binding registry) and which detector it uses
- [ ] 2. `StepExpressionPattern`: do not pin a text the SpecFlow detector reads as a Cucumber expression; council question if it differs from the Reqnroll detector
- [ ] 3. Move the pending proof; rerun shouty in the corpus

## See also

- `claude/tasks/CORPUSRUN-specflow.md` — corpus round 1
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md` — step 6 (S13), step 7
- `src/Unshackle.Core/Fixes/SpecFlow/Patterns/StepExpressionPattern.cs`
