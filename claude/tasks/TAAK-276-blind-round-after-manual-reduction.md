# TAAK-276 — Blind corpus round after the manual reduction

**Status**: step 1/4 in progress (agent)
**Branch**: `worktree-agent-acb7a9a79f3ea57be` · **Worktree**: `.claude/worktrees/agent-acb7a9a79f3ea57be`
**Created**: 2026-10-17
**Goal**: proof that TAAK-272..275 reduced manual work without regressions, on the same 18 blind repos plus a fresh set of unseen repos, with the numbers in README and on NuGet (release 0.2.0).

## Context for the next agent

- Baseline: `claude/tasks/CORPUSRUN-blind.md` (FA 6/6, MediatR 3/6, AutoMapper 1/6 migrated; 0 regressions).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Steps

- [→] 1. Rerun the 18 blind repos (`after` mode): migrated / partially migrated / Manual count / regressions per repo
- [ ] 2. 6–9 fresh unseen repos (never used during development) per library, same measurements
- [ ] 3. Every regression → its own task with a failing proof first; rerun until 0 regressions
- [ ] 4. Update README results + limitations, `docs/usage.md`; propose release 0.2.0 to the user

## Progress log

- Tool published from main `db05908` into `<corpus>/corpus-blind/_results/tool-db05908/`; previous `after` results moved to `_results/after-r4-old`.
- Step 1 run: `scratchpad` script calls `corpus-am.sh list after claude/tools/corpus-blind-list.txt` (JOBS=2, results in `<corpus>/corpus-blind/_results/after/`, log `_results/run18.log`).

- Step 1 DONE (tool `db05908`, 2026-10-18): all 18 `ok`, 0 build breaks, 0 real regressions. bookstore showed 1-2 "regressions" (OrderLine/Book `Update...Return_Ok` NotFound); a fresh untouched baseline run fails the same OrderLine test, so it is the known flaky shared test database. Results in `<corpus>/corpus-blind/_results/after/` (`summary`: `corpus-am.sh summary after`); copies `after-bookstore-run1`, `base-bookstore-orig`.
- Next: step 2 (select 6 new repos per library; candidate lists were gathered with `gh search code`, excluded against all CORPUSRUN/REGRESSIE files).

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
