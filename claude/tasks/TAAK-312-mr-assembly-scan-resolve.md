# TAAK-312 — MediatR: resolve AddMediatR scans of non-literal assemblies

**Status**: step 3/3 — implemented per council round 8 A2; waiting for the main session (shared-file lines, merge)
**Created**: 2026-10-22
**Goal**: Resolve the scanned assembly when AddMediatR gets it from a type parameter or a method parameter whose callers are all in the solution.

## Context for the next agent

- From `claude/tasks/ANALYSE-manual-round-2.md` (TAAK-306 rank 2). Repos: blind miru, starter (whole solutions blocked); stale mediatr evo1, evo2.
- Non-public handlers (ardalis, evo2) stay justified unless the generated code can see them; MediatR stays all-or-nothing, so one root here blocks a whole repo.
- Rule (council round 8, A2): Fixed when every caller in the solution passes the same literal `typeof(X).Assembly` and the method is non-public or in a non-packable project; callers in inactive `#if` or unknown lambdas count as unknown. Otherwise Manual (`AssemblyNotStaticallyKnown`).

## Steps

- [x] 1. Call shapes: miru `PipelineRegistry.AddHandlers<T>/AddPipeline<T>` (public, packable library `Miru`, many different `T` in tests); starter `InMemoryPublisherModule.AddInMemoryPublisher(assembly)` (internal) fed by public `PublisherModule.AddPublisher(Assembly)` called with `Assembly.GetExecutingAssembly()`. Both are Manual under A2.
- [x] 2. `AssemblyResolver` + `AssemblyResolution` in `Fixes/MediatR`; `AddMediatRPattern.RewriteAsync` uses it; proofs `resolved-type-parameter`, `type-parameter-different-assemblies`, `type-parameter-inactive-caller`; unit tests `Core/MediatR/AssemblyResolverTests.cs`; KingmaDoc REQ-9.
- [x] 3. Blind scan (read-only copy): miru 98 → 98 Manual, starter 11 → 11 Manual (both expected). Full corpus round not run.

## Decisions taken in the implementation

- Resolution only follows an `Assembly` parameter passed as a whole argument and a method type parameter; the X must be named literally (`typeof(X).Assembly` or explicit `<X>`), callers compared by X's assembly.
- The resolved assembly must be the scanning project or one of its references (Mediator's generator cannot see other projects).
- Unknown callers: inactive `#if` text naming the method anywhere in the solution, a method group / non-call reference, a call inside a lambda whose parent is not an argument, no callers at all.
- Packable = library without a test framework reference (`IsPackable` is not in the workspace); conservative.
- The currently Fixed internal method without a friend caller (`edge-assemblies`) stays Fixed when unresolved (no regression); a resolved assembly now also goes through the host-clash and hidden-handler checks.
- `AssemblyNotStaticallyKnown` is a reason-text prefix (`AssemblyResolution.AssemblyNotStaticallyKnown`), not a `KeptReason` value: `KeptReason` lives in `Models/` (not claimed) and a kept reason means "the rest goes ahead", which is wrong for this root blocker.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `src/Unshackle.Core/Fixes/MediatR/AssemblyResolver.cs`, `AssemblyResolution.cs`, `Patterns/AddMediatRPattern.cs` |
| **How**   | Pattern change + proofs + unit tests |
| **Why**   | Unblocks scans whose assembly is statically known through the callers |

## See also

- `claude/tasks/ANALYSE-manual-round-2.md` — ranking this task comes from
- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — parent task
- `claude/docs/council.md` — round 8, A2
- `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md` — REQ-9
- `claude/docs/INDEX.md` — documentation index
