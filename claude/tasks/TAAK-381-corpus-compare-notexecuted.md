# TAAK-381 — corpus-regression.py does not count Passed → NotExecuted as a regression

**Status**: open
**Created**: 2026-10-28
**Goal**: The corpus harness flags a test that passed before the fix and did not run after it.

## Context for the next agent

- `claude/tools/corpus-regression.py` `compare` excludes `NotExecuted` from regressions. In the SpecFlow round
  (`claude/tasks/CORPUSRUN-specflow.md`) the TAAK-377 regression (undefined step → NUnit inconclusive → TRX `NotExecuted`) showed as
  `regressions: []`; it was found only by reading the outcome counts.
- `NotExecuted` was probably excluded for tests skipped by environment. A Passed → NotExecuted change is still a changed outcome.

## Steps

- [ ] 1. Report Passed → NotExecuted in its own list (`not_run`) and in `summary`; selftest case
- [ ] 2. Recheck earlier corpus reports for such cases

## See also

- `claude/tasks/CORPUSRUN-specflow.md`
- `claude/tasks/TAAK-377-specflow-step-expression-cucumber-parameter.md`
