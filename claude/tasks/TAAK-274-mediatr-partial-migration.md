# TAAK-274 — MediatR partial migration per request type

**Status**: step 1/6 — not started (waits for TAAK-271)
**Created**: 2026-10-17
**Goal**: a few Manual findings no longer block a whole MediatR solution: request/notification types whose handlers and call sites can be migrated go to Mediator, the rest stays on MediatR, both registered side by side, and the app passes its own tests.

## Context for the next agent

- Today MediatR migrates all or nothing per solution (`MediatRFix.cs`): a half-migrated app sends a MediatR request to a handler that now implements Mediator's interface → runtime "no handler".
- Blind corpus: fullcart (2 justified Manual block 31 findings), miru (generic `AddMediatR` in a library method → Mediator's source generator cannot see handlers), starter (11 Manual, see TAAK-271).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Analysis

### New situation

Per message type one owner: its handler(s), behaviors that apply to it, and every `Send`/`Publish` call site move together, or nothing moves. Callers holding `IMediator`/`ISender` of MediatR that send a migrated type are rewritten to Mediator's `IMediator`; both `AddMediatR` and `AddMediator` stay registered while MediatR types remain. Open generic pipeline behaviors that apply to both worlds need an explicit decision (duplicate per world, or keep the types they touch on MediatR).

## Approach

1. Probe app: MediatR 12.5.0 and Mediator side by side in one DI container (name clashes `IMediator`/`ISender`, notification fan-out, behaviors order).
2. Ownership graph: message type ↔ handlers ↔ behaviors ↔ call sites (incl. generic and reflection-based sends → keep on MediatR).
3. Rewrite only migratable message types; namespace aliases where both libraries are used in one file.
4. miru: decide whether moving a generic `AddMediatR` to the host can be done with proof; otherwise principally manual (state why).

## Steps

- [ ] 1. KingmaDoc plan (feature `mediatr-partial-migration`) after reading TAAK-271
- [ ] 2. Probe app: side-by-side semantics
- [ ] 3. Ownership graph + unit tests
- [ ] 4. Rewrite + registration + project-file changes + proofs (`_Combined` mixed cases)
- [ ] 5. Corpus `after` on fullcart, miru, starter + earlier MediatR repos: 0 regressions
- [ ] 6. Verify, models, docs, commit

## Risks & edge cases

| Risk                                          | Mitigation                                                                                       |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| A send reaches the wrong mediator at run time | Ownership per message type includes every call site; unresolvable sends keep the type on MediatR |
| Behaviors run twice or not at all             | Probe measures order; proofs per behavior case                                                   |
| Name clashes between the two libraries        | Aliases, proven by compiling `after` against both assemblies                                     |

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-mediatr.md`
