# TAAK-382 — xunit3: new xunit.v3 analyzer warnings break TreatWarningsAsErrors builds

**Status**: step 1/3 — not started
**Created**: 2026-10-28
**Goal**: A v2 project with `TreatWarningsAsErrors` still builds after `fix --fix xunit3`.

## Context for the next agent

- Found in the first xunit3 corpus round (`claude/tasks/CORPUSRUN-xunit.md`, FluentValidation `f95dbd5`, xunit 2.2.0, `src/Directory.Build.props`
  sets `TreatWarningsAsErrors`). After the fix the build fails with 822 analyzer errors: 768 xUnit1051 (pass
  `TestContext.Current.CancellationToken`, new in v3), plus xUnit1010, xUnit1031, xUnit2000, xUnit2009, xUnit2012, xUnit2013 (the v2.2.0
  project had no `xunit.analyzers`; `xunit.v3` brings them).
- Not fixed in the corpus round (by agreement). Pending proof: `src/Unshackle.Tests/Proofs/_Pending/XunitV3/AnalyzerWarnings/cancellation-token.*`
  (one of two possible outcomes: rewrite to pass the token).
- Options: (a) rewrite xUnit1051 calls (pattern, the proof); (b) add `xUnit1051` (and analyzer ids new to the project) to `NoWarn` /
  `WarningsNotAsErrors` with an Info row; (c) Info row only. Council question.

## Steps

- [ ] 1. Council: rewrite (a), suppress (b) or Info (c); which ids (only v3-new ones such as xUnit1051, or every analyzer when v2 had none)
- [ ] 2. Build it; move the proof out of `_Pending`
- [ ] 3. Re-run FluentValidation in the corpus: build after fix, equal discovered-test count

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Fixes/XunitV3/` |
| **How**   | New pattern or project-file edit, per council |
| **Why**   | A migrated solution must build; TreatWarningsAsErrors is common |

## See also

- `claude/tasks/CORPUSRUN-xunit.md` — corpus round
- `claude/tasks/TAAK-341-xunit-v3.md` — the xunit3 fix
