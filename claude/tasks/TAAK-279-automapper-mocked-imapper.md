# TAAK-279 — AutoMapper: mocked IMapper no longer keeps whole map pairs on AutoMapper

**Status**: step 1/6 — not started
**Created**: 2026-10-18
**Goal**: a test that stubs `IMapper` (Moq/NSubstitute/FakeItEasy) only keeps **its class under test** on AutoMapper; every other call site of the same map pair migrates, with proof that the app and its own tests behave the same.

## Context for the next agent

- TAAK-273 (archived) measured that after TAAK-272/273 most remaining AutoMapper Manual in the blind corpus is caused by mock set-ups of `IMapper` (culinary 0 Fixed / 46 Manual, fiapgames, msb).
- Today (`src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs`, `IsMockSetUp`, reason "a mock set-up of IMapper in a test stays as it is, and so do the maps it stubs"): a stubbed pair keeps its `CreateMap` **and every call site** of that pair on AutoMapper.
- Proof rules: `claude/tasks/manual-reduction-queue.md`.

## Analysis

### Current situation

Ownership is per map pair: one owner (`ToB()` or AutoMapper). A stub forces the pair to AutoMapper everywhere.

### New situation (to be decided with proof)

Option A (preferred if provable): **both owners for a stubbed pair** — keep its `CreateMap` (the class under test still calls `IMapper.Map<B>`, its test still stubs it), and also generate `ToB()`; migrate the call sites in classes that are **not** under a mocked test. The two paths are equivalent by the existing proofs of `ToB()`. `AddAutoMapper` and the package stay while any map stays.
Option B: rewrite the test too (remove the stub, let the real mapping run) — changes what the test checks (canned value vs real mapping) → only if a probe shows the assertions still hold; otherwise principally manual.
Determining "class under test": the type whose constructor receives the mocked `IMapper` (`new Sut(mapper.Object)`, DI registration in the test), not a name heuristic.

## Approach

1. Probe on culinary/fiapgames (corpus under `<corpus>/…`, see `claude/tasks/CORPUSRUN-blind.md`): which classes are under mocked tests, which call sites of the same pairs live elsewhere.
2. KingmaDoc plan (feature `automapper-mocked-imapper`), design A vs B with measurements.
3. Implementation in `MapOwners` (+ generation of `ToB()` for kept pairs), unit tests, proofs incl. `_Combined` (stubbed class keeps `IMapper`, other class migrates).
4. Corpus `after` on culinary, msb, fiapgames, restapi, baseapi + the 14 AutoMapper corpus repos: **0 regressions**, Fixed/Manual before/after.
5. Verify, models, docs (README limitations).

## Steps

- [ ] 1. Probe: classes under mocked tests vs other call sites (counts per repo)
- [ ] 2. KingmaDoc plan + design decision with evidence
- [ ] 3. Implementation + unit tests
- [ ] 4. Proofs (`MapCall`, `_Combined`)
- [ ] 5. Corpus `after`: 0 regressions, before/after numbers
- [ ] 6. Verify, models, docs, archive

## Risks & edge cases

| Risk                                                            | Mitigation                                                                                           |
| --------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| A class under test is also used elsewhere with a real `IMapper` | It keeps `IMapper` everywhere (owner = class, not call site)                                         |
| Stub set up in a shared fixture / base test class               | Resolve the constructed SUT through the fixture; unresolvable → keep the pair on AutoMapper as today |
| Two owners drift apart                                          | Both come from the same `CreateMap`; proofs compare `ToB()` with AutoMapper                          |

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/archief/TAAK-272-automapper-partial-migration.md`, `claude/tasks/archief/TAAK-273-automapper-specific-blockers.md`
