# TAAK-274 — MediatR partial migration per request type

**Status**: step 1/6 — in progress (probe of step 2 done first)
**Branch**: `worktree-agent-a69be07f29be9cc9b` · **Worktree**: `.claude/worktrees/agent-a69be07f29be9cc9b` (under the repo root)
**Created**: 2026-10-17
**Goal**: a few Manual findings no longer block a whole MediatR solution: request/notification types whose handlers and call sites can be migrated go to Mediator, the rest stays on MediatR, both registered side by side, and the app passes its own tests.

## Context for the next agent

- 2026-10-17: picked up by an agent (Opus) in its own worktree; TAAK-271 is done.
- Probe (step 2) done: `<corpus>/corpus-blind/_results/274/probe/Side` (MediatR 12.5.0 + Mediator 3.0.2 in one container). Results in section "Probe" below.
- Design chosen (details in the plan once written): taint-and-retry. Run 1 = today's full fix. If Manual findings block, `MediatRFix : IPartialFix`
  plans a partial run: union-find over message types, handlers, behaviors, user base types, holders (fields/params/properties of
  MediatR IMediator/ISender/IPublisher) and DI statements; components touched by a Manual root (or by MediatR's own registration = INFRA)
  stay on MediatR. Kept references are pre-qualified as `global::MediatR.X` and annotated; patterns skip kept nodes; a partial-only pattern
  adds `AddMediator` next to the kept MediatR registration. Retry until no new blocking Manual (max N), else fall back to all-or-nothing.
- Next: KingmaDoc plan, then implementation (Core: `IPartialFix`, `PartialPlan`, Fixer loop; MediatR: ownership planner).

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

## Scope from TAAK-271 (measured)

- **fullcart** (2 roots, 31 blocked): `services.AddScoped<IMediator, Mediator>()` and Scrutor `Decorate(typeof(INotificationHandler<>), typeof(DispatchingIntegrationEventDecorator<>))`. Solvable only as partial migration: requests/queries (with their `IPipelineBehavior` registrations) move to Mediator, **notifications and their decorator stay on MediatR**. Classes that use both `Send` and `Publish` need both interfaces.
- **starter** (1 root, 10 blocked): `Starter.Common` and `Starter` both grant `InternalsVisibleTo` to `Starter.IntegrationTests`, and the test project calls `AddMediatR`. Probe (two Mediator hosts + ISV to a project that calls `AddMediator()`): CS0121, also with `options.Namespace`; builds when that project does not call `AddMediator`. Solvable by project-level partial migration: keep `Starter.Common` and the test project on MediatR, migrate the messages of `Starter` only.
- **miru** (2 roots, 96 blocked): **principally manual**. `AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TAssemblyOfType>())` is a framework API scanning its users' assemblies; Mediator's generator needs them at compile time (MSG0007). Even per message type every handler is reached through that call. Keep the reason text of `mediatr.add-mediatr` and add "the library's users register their own handlers". Step 4 of the approach below is therefore decided: state, do not attempt.
- SampleShop `CountQuery<T>` (generic message + generic handler): principally manual (no generic messages in Mediator; monomorphising changes the public type surface). 0 of 8 blocked repos.
- Unit of ownership: message type **and project** (a project that cannot host the generator, as in starter, keeps its types on MediatR).

## Probe (MediatR 12.5.0 + Mediator 3.0.2 side by side, `<corpus>/corpus-blind/_results/274/probe/Side`)

| Question | Result |
| --- | --- |
| Both registered (`AddMediatR` + `AddMediator`) | `MediatR.IMediator`/`ISender` → `MediatR.Mediator`; `Mediator.IMediator`/`ISender` → `Mediator.Mediator`: separate services, no conflict |
| Name clashes with `using MediatR; using Mediator;` | CS0104 on `IMediator`, `ISender`, `IPublisher`, `INotification`, `Unit` (and every other twin) |
| `Mediator` (MediatR's class) with a compilation-unit `using MediatR;` once Mediator.Abstractions is referenced | CS0118 (`Mediator` binds to the namespace); inside a namespace block it still binds to the class |
| Alias `using IMediator = MediatR.IMediator;` next to both usings | builds: an alias wins over imported namespaces (not usable for open generics) |
| Behaviour order | both libraries: registration order, first registered = outermost; MediatR behaviors never run for Mediator requests and vice versa |
| Notification fan-out | a type implementing both `INotification`s: MediatR's `Publish` reaches only MediatR handlers, Mediator's only Mediator handlers (handlers of one type must move together) |
| Wrong mediator | compiles through the `object` overloads and fails at run time: MediatR `Send(object)` → `ArgumentException` "does not implement IRequest"; MediatR `Publish(object)` → `ArgumentException`; Mediator `Send/Publish(object)` → `InvalidMessageException`. Typed calls do not compile (CS0311/CS1503) |

## Steps

- [→] 1. KingmaDoc plan (feature `mediatr-partial-migration`) after reading TAAK-271
- [x] 2. Probe app: side-by-side semantics
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
