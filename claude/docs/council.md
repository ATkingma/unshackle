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

### 2026-10-19 — implementer questions from TAAK-292 (round 3, unanimous after amendments)

| # | Question | Joint decision |
| --- | --- | --- |
| Q1 | Info line in the model | **Separate optional `FixedResult.Info`**, not counted as a warning; also in the JSON/markdown output. Putting it in `Warning` would make warning counts untrustworthy. |
| Q2 | How a kept lookup is reported | **Manual finding on the lookup call, not undoing the rest of the fix, with a typed reason code** (Architect) instead of a string prefix; the renderer prints `Stays on AutoMapper:`, and a test pins that rendered text (Guardian). The text names the failed rule with file:line. |
| Q3 | Literal reading of the rules | Accepted, with two changes. (a) A target-typed `new()` counts as a container through its converted type (`IServiceCollection`/`ServiceCollection`): not recognising it errs the unsafe way. (b) An `AddAutoMapper` inside a lambda counts as unconditional only when that lambda is passed directly to an unconditional registration or builder call; otherwise the lookup is kept, with reason code `ConditionalOrDeferredRegistration` and the file:line of the `AddAutoMapper` call/lambda. Inactive `#if` code fails rule 1, and a container filled in another method keeps the lookup. |
| Q4 | Which removals warn | **Every removed lookup warns**, `GetRequiredService` included: removing it changes how a mis-wired host fails. |
| — | fullcart reason text says "Autofac's RegisterGenericDecorator" | **Fix before 0.2.0**: name Scrutor's `Decorate(typeof(INotificationHandler<>), …)`. |

Condition (Pragmatist): rerun the corpus `after` on the six AutoMapper repos, 0 regressions. Implementation: TAAK-293.

### 2026-10-19 — implementer questions from TAAK-293 (round 4, unanimous)

| # | Question | Joint decision |
| --- | --- | --- |
| Q1 | `AddAutoMapper` inside a local function | **Deferred: keep the lookup.** A local function may never run, and it is not a lambda passed directly to a registration call. |
| Q2 | Other kept findings still use a string prefix | **Move to reason codes after 0.2.0** (TAAK-295). Users see no change; the tests pin the rendered text. |
| Q3 | Recognising registration/builder types | **Match on the fully qualified metadata name, namespace included** (`Microsoft.Extensions.*`, `Microsoft.AspNetCore.*`, `Microsoft.Azure.Functions.*`), not on a bare name and not on the assembly. A lookalike name would wrongly remove a lookup. Add `HostApplicationBuilder` and the isolated Functions builders (`FunctionsApplicationBuilder`, `IFunctionsWorkerApplicationBuilder`). Before 0.2.0. |
| Q4 | Scrutor recognised by syntax only | **Require that `Decorate` resolves to a method in the `Scrutor` namespace**; use the syntax match only when the symbol does not resolve. A user's own `Decorate` would otherwise cause a false partial. Before 0.2.0. |

Nothing here blocks 0.2.0 outright; Q3 and Q4 are small and land before it (TAAK-294), with the corpus `after` at 0 regressions.

## See also

- `claude/docs/agent-werkwijze.md` — task management and rules of conduct
- `claude/docs/overdracht.md` — handover
- `claude/tasks/archief/TAAK-292-council-follow-up.md` — implementation of these decisions
- `claude/tasks/archief/TAAK-293-council-round-3.md` — implementation of the round-3 decisions
- `claude/tasks/archief/TAAK-294-council-round-4.md` — round-4 changes before 0.2.0
- `claude/tasks/TAAK-295-kept-reason-codes.md` — reason codes for all kept findings (after 0.2.0)
