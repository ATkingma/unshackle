# Decision council

When an agent faces a choice that is not settled by the code, a task or an earlier user decision, it does not decide alone and does not
wait for the user: it puts the choice to the council. The council is three sub-agents with different personalities. They give a verdict
each; when they disagree, the main session shares the other arguments in a new round until all three accept one joint decision.
Decisions are recorded below. The user can overrule any of them.

## Members

| Member | Personality | Asks |
| --- | --- | --- |
| **The Guardian** | Cautious, correctness-first, blunt. Rather Manual than a rewrite that may break at run time. | Can this silently change behaviour in the user's code? |
| **The Pragmatist** | User advocate, results-driven, numbers-focused. A clear warning often beats refusing. | How much hand work does the developer have left? |
| **The Architect** | Principled, calm, precise; thinks in predictable rules, single responsibility, GoF/refactoring.guru. | Can a user predict this rule from the report? |

## Procedure

1. The main session writes the decision(s) with context and file pointers, and spawns the three members (read-only, e.g. `Explore`), each with its personality in the prompt.
2. Each member returns a choice per decision with a short reasoning, plus concerns.
3. Unanimous → decided. Otherwise the main session sends each dissenting member the others' arguments and asks for one rule all can accept; that proposal goes back to the others (accept / reject / amendment) until all accept.
4. The main session records the decision here (date, votes, rule) and turns follow-up work into a task.

## Decisions

### 2026-10-19 — open points after TAAK-288..291

| # | Decision | Votes (G / P / A) | Joint decision |
| --- | --- | --- | --- |
| 1 | starter (MediatR): run-time assembly scan via public `PublisherModule.AddPublisher(Assembly)` | Manual / Manual / Manual | **Principally manual.** The fix would change a public API and move registration to hosts we cannot see. |
| 2 | fullcart (MediatR): Scrutor `Decorate(typeof(INotificationHandler<>), …)` | partial / partial / partial | **Stays partial.** Under Mediator's generator the decorator becomes a plain handler: silent behaviour change. The report names the decorator. |
| 3 | unused-imapper: remove `GetService<IMapper>()` + null-guard | keep lookup / remove / remove → round 2 | **Scoped removal** (see below). |
| 4 | Empty constructors and factory classes left after unused-imapper | leave / leave + Info / leave + Info | **Leave them; report an Info line** ("`MapperHelper` is now empty"). Removing types is a separate refactoring; an opt-in rule later if users ask. |

**Decision 3, joint rule (round 2).** Remove the lookup and its null-guard, with a warning naming file:line, only when all hold:

1. an `AddAutoMapper` call is in the lookup's own project or in a project that transitively depends on it (`GetProjectsThatTransitivelyDependOnThisProject`);
2. that call is unconditional (not inside `if`, `?:`, `switch`, `#if` or a loop);
3. no project in that set has a `new ServiceCollection()`, `Host.CreateApplicationBuilder` or `WebApplication.CreateBuilder` whose own method body lacks `AddAutoMapper` (purely syntactic, Architect's amendment).

Otherwise keep the lookup; the report says which condition failed, with file:line (Pragmatist's amendment). This replaces the solution-wide `bool registered` in `MapperCleanup`.

**Concerns raised for 0.2.0** (follow-up in TAAK-292):

- Rerun the blind corpus on current main (the table predates the redone TAAK-288..290); any "build fails after fix" blocks the release.
- `MapperCleanup` plan cache keyed by documentation comment id: test overloads/partial types across projects; check no stale plan after earlier fixes in the same run.
- Removing call-site arguments of a public constructor is an API change for external consumers: consider limiting to non-public types or non-packable projects.
- Enforce the order call patterns → unused-imapper → AddAutoMapper explicitly; one end-to-end test where a single remaining `IMapper` use keeps the registration.
- Release notes list Fixed/Manual per library and every Manual/partial case.

## See also

- `claude/docs/agent-werkwijze.md` — task management and rules of conduct
- `claude/docs/overdracht.md` — handover
- `claude/tasks/TAAK-292-council-follow-up.md` — implementation of these decisions
