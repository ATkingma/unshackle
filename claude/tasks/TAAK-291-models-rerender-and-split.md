# TAAK-291 — Docs: re-render pattern models, split decision trees over 12 arrows

**Status**: step 1/4 — not started
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- Some `-modellen.md` figures were not re-rendered after TAAK-280/282 (only tables updated) and several decision trees have more than 12 arrows (known open issue).
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Re-render every pattern/feature model whose source changed (`kingmadoc render`), split crowded decision trees into figures of at most 12 arrows, keep the text in sync with the figures.

## Steps

- [ ] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on — (docs only): build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
