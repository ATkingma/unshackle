# TAAK-296 — Blind corpus rerun on current main before 0.2.0

**Status**: step 1/3 — not started
**Created**: 2026-10-19
**Goal**: council condition for 0.2.0: rerun every repo of the blind round (`claude/tasks/CORPUSRUN-blind.md`) with the tool from current main; any "build fails after fix" or test regression blocks the release.

## Context for the next agent

- The CORPUSRUN-blind round-5 table predates TAAK-288..294 (redone/added on 2026-10-19).
- Repos already cloned: `<corpus>/corpus-blind/src` (afripay, assistiva, cleanmicro, culinary, encryptednotes, estore, fullcart, msb, pi22317, starter). Clone the other repos of the blind list (FA, MediatR, AutoMapper) there too. Harness `claude/tools/corpus-am.sh` with `FIX=<library>`.
- A blocker found here: write it down with its root cause, fix it in this task if it is small and clear (with proof), otherwise make a new task and put any design choice to the council (`claude/docs/council.md`).

## Steps

- [ ] 1. Build the tool from main; baseline (`base`) for repos without one
- [ ] 2. `after` on all repos; table in CORPUSRUN-blind.md (round 6) with Fixed/Manual, tests before → after, regressions
- [ ] 3. Fix or task-ify blockers; verify; release go/no-go line for 0.2.0 in the table summary

## See also

- `claude/tasks/CORPUSRUN-blind.md`, `claude/docs/council.md`, `claude/tasks/TAAK-019-lancering.md`
