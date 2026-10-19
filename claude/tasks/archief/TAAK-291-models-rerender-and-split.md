# TAAK-291 — Docs: re-render pattern models, split decision trees over 12 arrows

**Status**: done (2026-10-19)
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- Some `-modellen.md` figures were not re-rendered after TAAK-280/282 (only tables updated) and several decision trees have more than 12 arrows (known open issue).
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Re-render every pattern/feature model whose source changed (`kingmadoc render`), split crowded decision trees into figures of at most 12 arrows, keep the text in sync with the figures.

## Steps

- [x] 1. Measured: 32 D2 sources in `docs/projectinfo/patterns/img/` had no PNG/SVG (15 AutoMapper models written while d2 was missing, plus inline `d2` blocks in add-automapper, kept-map, unused-imapper, timespan-extensions), 2 were stale (create-map-3, fa-be-greater-less-3); 38 decision trees had more than 12 arrows. No pattern code touched, so no KingmaDoc plan.
- [x] 2. Rendered 20 models with `kingmadoc render` (the "follow with the next render" notes removed). Split the 38 trees by a topological prefix into chained figures Na, Nb(, Nc) of at most 12 arrows each, joined by "Continued in Figure Nb" / "From Figure Na" connector nodes (fa-throw and automapper-constructor in three parts); captions say so; leaf tables unchanged. Line endings kept LF. Also: `mediatr.request-handler-delegate` description (and `docs/patterns.md`) now names real lambdas and Moq mocks; starter reason in CORPUSRUN-blind round 5 updated to the TAAK-290 finding; handover updated.
- [x] 3. Docs only: no corpus run.
- [x] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## Open points

- Sequence diagrams (figure 2) with more than 12 arrows (about 25, mostly FA) are not split; kingmadoc still warns on them.
- About 23 models are tables only, without figures (most MediatR patterns, several AutoMapper and FA ones, some with Mermaid); drawing them needs facts from the code per pattern.

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
