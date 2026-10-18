# TAAK-276 — Blind corpus round after the manual reduction

**Status**: steps 1-3 done (agent, 2026-10-18); step 4 only as a proposal below
**Branch**: `worktree-agent-acb7a9a79f3ea57be` · **Worktree**: `.claude/worktrees/agent-acb7a9a79f3ea57be`
**Created**: 2026-10-17
**Goal**: proof that TAAK-272..275 reduced manual work without regressions, on the same 18 blind repos plus a fresh set of unseen repos, with the numbers in README and on NuGet (release 0.2.0).

## Context for the next agent

- Baseline: `claude/tasks/CORPUSRUN-blind.md` (FA 6/6, MediatR 3/6, AutoMapper 1/6 migrated; 0 regressions).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Steps

- [x] 1. Rerun the 18 blind repos (`after` mode): migrated / partially migrated / Manual count / regressions per repo
- [x] 2. 6–9 fresh unseen repos (never used during development) per library, same measurements
- [x] 3. Every regression → its own task with a failing proof first; rerun until 0 regressions
- [→] 4. README results table updated (done); `docs/usage.md` and release 0.2.0 left for the user (proposal below)

## Progress log

- Tool published from main `db05908` into `<corpus>/corpus-blind/_results/tool-db05908/`; previous `after` results moved to `_results/after-r4-old`.
- Step 1 run: `scratchpad` script calls `corpus-am.sh list after claude/tools/corpus-blind-list.txt` (JOBS=2, results in `<corpus>/corpus-blind/_results/after/`, log `_results/run18.log`).

- Step 1 DONE (tool `db05908`, 2026-10-18): all 18 `ok`, 0 build breaks, 0 real regressions. bookstore showed 1-2 "regressions" (OrderLine/Book `Update...Return_Ok` NotFound); a fresh untouched baseline run fails the same OrderLine test, so it is the known flaky shared test database. Results in `<corpus>/corpus-blind/_results/after/` (`summary`: `corpus-am.sh summary after`); copies `after-bookstore-run1`, `base-bookstore-orig`.
- Next: step 2 (select 6 new repos per library; candidate lists were gathered with `gh search code`, excluded against all CORPUSRUN/REGRESSIE files).

- Step 2 selection DONE (2026-10-18, before any `after` run): candidates from `gh search code` (csproj `Include="<Lib>" Version="<free major>."`), checked against all CORPUSRUN/REGRESSIE files, sln + test project present, then baseline (`base` mode) must build and run tests. Pins in `claude/tools/corpus-blind-repos.txt` ("round 5"), list `claude/tools/corpus-blind-list-r5.txt` (all candidates), `corpus-blind-list-r5-selected.txt` (the 18 chosen).
  - Rejected at baseline: supertext (build fails), octopus (tests hang >1 h), innosetup, dotnettemplate, passwordmanager (restore fails), orderservice, policeevents, expensestracker, bugtracker, cmartlineupbuilder (build fails), puppeteer (flaky browser tests), minifyurl/otuscleanarch/fijournal/mycrm/lulusia/techchallenge (0 tests discovered).
- Step 2 `after` run starting (tool `db05908`), results `<corpus>/corpus-blind/_results/after/`.

## Results (see `claude/tasks/CORPUSRUN-blind.md`, "Round 5")

- Step 1: 18 old repos, 0 regressions, 0 build breaks. Step 2: 18 new repos, 5 build breaks from 4 defects, 0 test regressions on the other 13. Step 3: TAAK-280..283 with pending proofs (`_Pending`, verified failing); not fixed here.
- Proposal for 0.2.0: first fix TAAK-280..283 (small, each has a proof) and rerun opentws, serilogmem, assistiva, encryptednotes, estore; then release 0.2.0 with the README table; mention in the release notes that FA and MediatR migrate fully on about half of unseen repos and AutoMapper mostly partially.

## Changed files

- `claude/tasks/CORPUSRUN-blind.md`, `README.md`, `claude/tools/corpus-blind-repos.txt`, `claude/tools/corpus-blind-list-r5*.txt`, `claude/tasks/manual-reduction-queue.md`, `claude/tasks/TAAK-280..283-*.md`, pending proofs under `src/Unshackle.Tests/Proofs/*/_Combined/_Pending/`.

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
