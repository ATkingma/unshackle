# TAAK-272 — AutoMapper partial migration

**Status**: step 1/6 — not started (waits for TAAK-271)
**Created**: 2026-10-17
**Goal**: one Manual map no longer blocks a whole solution: every map that can be migrated with proof is migrated, the rest keeps running on AutoMapper 14 (free), and the app still builds and passes its own tests.

## Context for the next agent

- Today AutoMapper migrates all or nothing per solution (`AutoMapperFix.cs`, `CreateMapPattern.cs`, `NestedMaps.cs`, `MapBlockers`): a half-migrated app would call `IMapper.Map<B>` for a pair whose `CreateMap` was removed → runtime `AutoMapperMappingException`.
- Partial migration must keep, per type pair, **one** owner: either the generated `ToB()` or AutoMapper. A pair stays on AutoMapper when it is Manual or when an AutoMapper map still needs it (nested maps, `Include`, inheritance).
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

- [ ] 1. KingmaDoc plan (feature `automapper-partial-migration`) after reading TAAK-271
- [ ] 2. Probe program: mixed app semantics
- [ ] 3. Dependency graph + closure (unit tests)
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
