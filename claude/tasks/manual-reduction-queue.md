# Manual-reduction queue (after 0.1.0)

Goal (user, 2026-10-17): **avoid as much manual work as possible, always with proof.** Every rewrite keeps the
behaviour provably the same (proofs, unit tests, corpus round with the projects' own tests, 0 regressions).
What stays Manual must be _principally_ manual (no equivalent code exists), and its reason must say so.

Baseline (blind corpus, `claude/tasks/CORPUSRUN-blind.md`): FA 6/6 migrated; MediatR 3/6 (blocked: fullcart, miru,
starter); AutoMapper 1/6 (blocked: culinary, msb, fiapgames, restapi, baseapi). Most blocking comes from the
all-or-nothing rule per solution, not from the number of Manual findings.

| #   | Task     | What                                                                                    | Depends on | Status |
| --- | -------- | --------------------------------------------------------------------------------------- | ---------- | ------ |
| 1   | TAAK-271 | Analyse every Manual finding in the 8 blocked repos (+ SampleShop)                      | —          | done   |
| 2   | TAAK-272 | AutoMapper partial migration: migrate what is safe, keep AutoMapper for the rest        | 271        | done (SampleShop FA Manual 19 → 9) |
| 3   | TAAK-273 | AutoMapper specific blockers: cycles, unflattening, null sources, private setters       | 271, 272   | open   |
| 4   | TAAK-274 | MediatR partial migration per request type (MediatR and Mediator side by side)          | 271        | in progress (agent)   |
| 5   | TAAK-275 | FluentAssertions: custom assertion classes → Shouldly                                   | 271        | in progress (agent)   |
| 6   | TAAK-276 | Blind corpus round(s): 0 regressions, measure Manual before/after; README/NuGet numbers | 272–275    | open   |
| 7   | TAAK-277 | FluentAssertions residual Manual patterns (proposed by TAAK-271; inventory first)       | 271, 275   | open   |

Statuses: `open` · `in progress (agent)` · `done` · `blocked (reason)`.

## Proof rules (every task)

- Measure library behaviour with a probe program against the real old version (FA 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0) before choosing a rewrite.
- Proofs per case (`before`/`after`/`manual`), `_Combined` cases for cooperation, unit tests for new logic.
- Corpus: scan before/after on the affected repos; `after` mode (fix → build → own tests) must show 0 regressions.
- KingmaDoc plan/models/verify for every pattern touched; hook `.claude/hooks/check-pattern-docs.sh` silent.
- Push only after the worktree check (`claude/docs/agent-werkwijze.md` §6), then watch CI until green.

## See also

- `claude/tasks/CORPUSRUN-blind.md`, `claude/tasks/CORPUSRUN-automapper.md`, `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/docs/patroon-loop.md` — workflow per pattern
