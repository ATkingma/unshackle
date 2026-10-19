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
| 2   | TAAK-272 | AutoMapper partial migration: migrate what is safe, keep AutoMapper for the rest        | 271        | done   |
| 3   | TAAK-273 | AutoMapper specific blockers: cycles, unflattening, null sources, private setters       | 271, 272   | done (cycles, unflattening, null source, ToMappedString fixed; private setters and broken mappings principally manual; remaining keeps are mock stubs) |
| 4   | TAAK-274 | MediatR partial migration per request type (MediatR and Mediator side by side)          | 271        | done (fullcart migrates partly; starter/miru stay: their root is `AddMediatR`) |
| 5   | TAAK-275 | FluentAssertions: custom assertion classes → Shouldly                                   | 271        | done   |
| 6   | TAAK-276 | Blind corpus round(s): 0 regressions, measure Manual before/after; README/NuGet numbers | 272–275    | open   |
| 7   | TAAK-277 | FluentAssertions residual Manual patterns (proposed by TAAK-271; inventory first)       | 271, 275   | done   |
| 8   | TAAK-279 | AutoMapper: a mocked IMapper only keeps its class under test on AutoMapper              | 272, 273   | done (option A: shared pairs; culinary 0→2, msb 3→8 Fixed, 0 regressions) |
| 9   | TAAK-280 | Regression (blind round 5): `#region` around removed `CreateMap` statements (CS1028)    | 276        | done   |
| 10  | TAAK-281 | Regression: FA using inside `#if`/`#else` removed, stray `#endif` (CS1028)              | 276        | open   |
| 11  | TAAK-282 | Regression: generated mapping omits `required` destination members (CS9035)            | 276        | done   |
| 12  | TAAK-283 | Regression: `using FluentAssertions.Common;` stays after the package is removed (CS0246) | 276        | open   |

| 9   | TAAK-285 | MediatR: `AddMediatR(Action<MediatRServiceConfiguration>)` overload | round 5 | open |
| 10   | TAAK-286 | AutoMapper: maps in `new MapperConfiguration(cfg => …)` outside a Profile | round 5 | open |
| 11   | TAAK-287 | FluentAssertions residual Manual, round 2 (inventory first) | round 5 | open |
| 12   | TAAK-288 | MediatR: `RequestHandlerDelegate` with a real delegate in tests | round 5 | open |
| 13   | TAAK-289 | AutoMapper: `AddAutoMapper` stays because `IMapper` is still injected | round 5 | open |
| 14   | TAAK-290 | MediatR: starter still blocked (`AddMediatR` + `InternalsVisibleTo`) | round 5 | done (principally manual: run-time assembly scan via a public wrapper; reason now names it) |
| 15   | TAAK-291 | Docs: re-render pattern models, split decision trees over 12 arrows | round 5 | open |

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
