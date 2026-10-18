# TAAK-287 — FluentAssertions residual Manual, round 2 (inventory first)

**Status**: step 1/4 — not started
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- FA migrates fully on 5/12 blind repos; the rest is partial: mihome (`Invoking` on non-trivial receivers, `.And` chains), catalogapi (`BeEquivalentTo` on a list, `ThrowAsync` on another delegate), buildenator 46, bearded 80, ogooreck 9, xrepo 9 Manual left.
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Scan these repos, group Manual by root cause, then probe FA 7.2.2 per cause and rewrite what is provably the same (same pass/fail); state principally-manual causes (e.g. equivalency options Shouldly lacks) in the reason. Start with the causes that unblock the most findings.

## Steps

- [ ] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on mihome, catalogapi, buildenator, bearded, ogooreck, xrepo: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
