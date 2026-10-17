# TAAK-272 — AutoMapper partial migration

**Status**: step 3/6 — in progress
**Created**: 2026-10-17
**Branch**: `worktree-agent-a50ecf5da4db68373` (worktree `.claude/worktrees/agent-a50ecf5da4db68373` of the main checkout)
**Goal**: one Manual map no longer blocks a whole solution: every map that can be migrated with proof is migrated, the rest keeps running on AutoMapper 14 (free), and the app still builds and passes its own tests.

## Context for the next agent

- Today AutoMapper migrates all or nothing per solution (`AutoMapperFix.cs`, `CreateMapPattern.cs`, `NestedMaps.cs`, `MapBlockers`): a half-migrated app would call `IMapper.Map<B>` for a pair whose `CreateMap` was removed → runtime `AutoMapperMappingException`.
- Partial migration must keep, per type pair, **one** owner: either the generated `ToB()` or AutoMapper. A pair stays on AutoMapper when it is Manual or when an AutoMapper map still needs it (nested maps, `Include`, inheritance).
- 2026-10-17: picked up by an agent (Opus) in a worktree branch, after TAAK-271.
- Done: plan (step 1), probe (step 2, results under `<corpus>/corpus-blind/_results/272/probe/output.txt`).
- Design (step 3, in progress): new `Fixes/AutoMapper/Patterns/MapOwners.cs` = solution-wide plan, cached per `Solution` instance.
  Nodes = CreateMap statements in profile constructors (forward + ReverseMap pairs). Seeds (kept): statement problems (CreateMapPattern.Rewrite
  in analysis mode, per statement instead of whole profile), profile-level problems, calls the call patterns cannot rewrite (same
  `RewriteCore` code), calls inside classes whose IMapper is mocked in tests. Closure fixpoint: kept node → its approximate AutoMapper needs
  (pairs among member/lambda/Include types) kept; migratable node whose generated ToB uses (recorded via `NestedMaps.Recording`) a kept pair → kept.
  Then profiles re-rewritten with the keep set to get generated method info (replaces `MapCallPattern.GeneratedAsync`/`MapBlockers`), loop until stable.
  Fixer/IFix: `IFix.AllowsPartial(Finding)` (default false); Fixer undoes only when a Manual finding is not allowed; AutoMapperFix allows
  everything except internal errors, overlaps, inactive-#if rows and encoding rows.
  New `MapperCleanupPattern` (automapper.mapper-cleanup): removes private IMapper field/ctor param/assignment and the `new C(...)` arguments.
  Mock set-ups (`m => m.Map<B>(It.IsAny<A>())`, `Arg.*`, `.Returns`) are never rewritten.
- Next: implement the above (CreateMapPattern refactor first), unit tests `MapOwnersTests`, `_Combined/partial-*` proofs.
- Blind corpus: 5 of 6 AutoMapper repos blocked (`claude/tasks/CORPUSRUN-blind.md`). Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Analysis

### Current situation

One Manual `CreateMap` anywhere → every `Map`/`ProjectTo` call and every profile stays untouched.

### New situation

Closure per pair: a pair is migrated only if it and every pair it depends on are migratable; its `CreateMap` is removed, its calls become `ToB()`/`Select(Projection)`. Remaining pairs keep their `CreateMap` in the (trimmed) profile; `AddAutoMapper` and the package stay while any map remains. Configuration tests (`AssertConfigurationIsValid`) keep working on the trimmed profiles.

## Approach

1. Probe program (AutoMapper 14): trimmed profile + some calls via `ToB()` and some via `IMapper` in one app behaves the same as the original (incl. `AssertConfigurationIsValid`).
2. Dependency graph of pairs (nested, `Include`/`IncludeBase`, `ReverseMap`, `ProjectTo` expansions); compute the migratable closure.
3. Rewrite only the closure; remove only its `CreateMap` lines; keep profiles/registration when non-empty; project file keeps AutoMapper when maps remain.
4. Report: findings outside the closure say "kept on AutoMapper because …" (not a blocker anymore).

## Scope from TAAK-271 (measured)

- **Granularity is one `CreateMap` pair, not a `Profile` class.** Culinary keeps 26 maps in one `MappingProfile`; today one Manual map makes every call say "in profile MappingProfile, which this fix does not rewrite".
- **New: `IMapper` clean-up** (7 of the 22 roots: fiapgames 5, culinary 1, msb 1; SampleShop 1). When every `Map`/`ProjectTo` call of a class is rewritten, remove the `IMapper` field, constructor parameter and the arguments at the callers, so `AddAutoMapper` can go. Tests that set up `Mock<IMapper>` (culinary) stay hand work: report them as such.
- The principally manual roots (msb `Map<OrderModel>(AddOrderDto)` without a map, restapi `List<DayEntity>` → `List<int>`, baseapi private setters without constructor) must only keep **their** pair on AutoMapper, not the whole solution.
- Depends on 273 for most of the corpus gain: after partial migration only A1-A3, A5 remain as blockers for the migratable remainder.

## Steps

- [x] 1. KingmaDoc plan (feature `automapper-partial-migration`) after reading TAAK-271: `docs/projectinfo/features/automapper-partial-migration-plan.md`
- [x] 2. Probe program: mixed app semantics (`<corpus>/corpus-blind/_results/272/probe`, AutoMapper 14.0.0, 15/15 OK):
  trimmed profile passes `AssertConfigurationIsValid`; kept pairs through `IMapper` (incl. nested, collections, `Map<B>(object)`) and migrated
  pairs through `ToB()`/`ToA()` (ReverseMap) give the same JSON as the original; a call left on `IMapper` for a removed pair throws
  `AutoMapperMappingException`; a kept map without its nested `CreateMap` fails the configuration check and throws at run time (an
  extension method does not help); a kept `Include<Dog, DogDto>` needs `CreateMap<Dog, DogDto>`; an emptied profile is valid.
- [→] 3. Dependency graph + closure (unit tests)
- [ ] 4. Rewrite/trim/registration/project-file changes + proofs (`_Combined` mixed cases)
- [ ] 5. Corpus `after` on the 5 blocked repos + the 14 earlier AutoMapper repos: 0 regressions; Fixed/Manual before/after
- [ ] 6. Verify, models, docs (README limitations), commit

## Risks & edge cases

| Risk                                                                                         | Mitigation                                                                   |
| -------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Pair migrated but still reached through `IMapper` at run time (reflection, generic `Map<T>`) | Keep the pair on AutoMapper when any call site cannot be resolved statically |
| `AssertConfigurationIsValid` fails on a trimmed profile                                      | Probe + proof; keep the test, trim consistently                              |
| Nested map split across owners                                                               | Closure includes all dependencies                                            |

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/archief/TAAK-271-analyse-manual-blockers.md`
