# TAAK-276 — Blind corpus round after the manual reduction

**Status**: step 1/4 — not started (waits for TAAK-272..275)
**Created**: 2026-10-17
**Goal**: proof that TAAK-272..275 reduced manual work without regressions, on the same 18 blind repos plus a fresh set of unseen repos, with the numbers in README and on NuGet (release 0.2.0).

## Context for the next agent

- Baseline: `claude/tasks/CORPUSRUN-blind.md` (FA 6/6, MediatR 3/6, AutoMapper 1/6 migrated; 0 regressions).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Steps

- [ ] 1. Rerun the 18 blind repos (`after` mode): migrated / partially migrated / Manual count / regressions per repo
- [ ] 2. 6–9 fresh unseen repos (never used during development) per library, same measurements
- [ ] 3. Every regression → its own task with a failing proof first; rerun until 0 regressions
- [ ] 4. Update README results + limitations, `docs/usage.md`; propose release 0.2.0 to the user

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
