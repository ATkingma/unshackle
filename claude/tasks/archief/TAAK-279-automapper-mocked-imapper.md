# TAAK-279 — AutoMapper: mocked IMapper no longer keeps whole map pairs on AutoMapper

**Status**: ✅ done — 2026-10-18
**Created**: 2026-10-18
**Branch**: `worktree-agent-a1ab1bc6d7ec0994b` (worktree `.claude/worktrees/agent-a1ab1bc6d7ec0994b`)
**Goal**: a test that stubs `IMapper` (Moq/NSubstitute/FakeItEasy) only keeps **its class under test** on AutoMapper; every other call site of the same map pair migrates, with proof that the app and its own tests behave the same.

## Context for the next agent

- TAAK-273 (archived) measured that after TAAK-272/273 most remaining AutoMapper Manual in the blind corpus is caused by mock set-ups of `IMapper` (culinary 0 Fixed / 46 Manual, fiapgames, msb).
- Today (`src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs`, `IsMockSetUp`, reason "a mock set-up of IMapper in a test stays as it is, and so do the maps it stubs"): a stubbed pair keeps its `CreateMap` **and every call site** of that pair on AutoMapper.
- Proof rules: `claude/tasks/manual-reduction-queue.md`.
- 2026-10-18: picked up by an agent (Claude Opus) in the worktree above. Corpus dir `<corpus>/t279` (`tool-before` = main `4392e29`,
  `run-before.sh` scans the five blocked repos (`five.txt`) and the 14 usable AutoMapper corpus repos (`am.txt`) into `blind-b`/`am-b`;
  `base` results are linked from corpus-blind / corpus-automapper).

## Measurement (step 1)

Counted on the sources (`<corpus>/corpus-blind/src`, `<corpus>/corpus-automapper/src`): mocks of `IMapper`, the class whose
constructor receives the mock (`new C(..., mock.Object)` in the test), and the call sites of the stubbed pairs elsewhere.

| Repo | Test classes mocking `IMapper` | Classes under test (constructor gets the mock) | Map calls in them | Call sites of stubbed pairs in other classes |
| --- | ---: | --- | ---: | --- |
| culinary | 5 | AccountManager, GroceryManager, MealPlannerManager, RecipeManager, ReviewManager | 21 | PreferenceManager:32 (`Preference → PreferenceDto` list, stubbed in AccountManagerTests:183) |
| msb | 6 | AddBoxCommandHandler, AddShelfCommandHandler, AddAddressCommandHandler, BoxController, WarehouseController (AddOrderCommandHandlerTests: construction commented out → unresolvable) | 9 | OrderController:108 (`BoxDto → BoxModel`); OrderController:78 (`OrderModel → OrderDto`) waits on `BoxDto → BoxModel` |
| fiapgames | 0 | — | — | — (its 9 Manual are principal: private setters) |
| restapi, baseapi | 0 | — | — | — |
| AutoMapper corpus (14) | dasblog 1 (ombi: commented out) | dasblog BlogPostViewModelCreator | 1 | dasblog BlogPostController:158 (same pair; its profile has several constructors, so it stays for that reason anyway) |

What the mocked tests check (option B): culinary `Assert.Equal(expectedAccount, result.Value)` on a canned DTO whose `AccountId` is set
while the source is `new Account()` (a real mapping gives `Guid.Empty` and another instance), and 5× `Verify(m => m.Map<…>(…), Times.Once)`;
msb `Assert.Equal(warehouseDto, returnedWarehouseDto)` on the canned instance. A real mapping would fail or remove these checks, so
**option B does not keep the tests' meaning** → option A. The gain is the call sites outside the classes under test (culinary 1, msb 1–2,
dasblog 0–1); the stubbed `CreateMap`s stay while their class under test calls `IMapper`, so no repo becomes AutoMapper-free.

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

- [x] 1. Probe: classes under mocked tests vs other call sites (counts per repo)
- [x] 2. KingmaDoc plan + design decision with evidence
- [x] 3. Implementation + unit tests
- [x] 4. Proofs (`MapCall`, `_Combined`)
- [x] 5. Corpus `after`: 0 regressions, before/after numbers
- [x] 6. Verify, models, docs, archive

## Risks & edge cases

| Risk                                                            | Mitigation                                                                                           |
| --------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| A class under test is also used elsewhere with a real `IMapper` | It keeps `IMapper` everywhere (owner = class, not call site)                                         |
| Stub set up in a shared fixture / base test class               | Resolve the constructed SUT through the fixture; unresolvable → keep the pair on AutoMapper as today |
| Two owners drift apart                                          | Both come from the same `CreateMap`; proofs compare `ToB()` with AutoMapper                          |

## Corpus (step 5)

Before scans done (tool `4392e29`, `<corpus>/t279/{blind-b,am-b}/_results/scan`): culinary 0/46, msb 3/32, fiapgames 25/9, restapi 0/9,
baseapi 5/3; AutoMapper corpus unchanged from earlier rounds (eshoponweb 4/0, the rest 0 Fixed: dasblog 0/61, ...).
First scan with the step-4 tool showed culinary `GroceryManager` migrated: its test constructs it with a Moq mock that stubs nothing
(a loose mock returns null), so the rule was widened: **every class a test constructs with a mocked `IMapper` keeps all its Map calls**
(their pairs are shared). Scan after (final tool): culinary 2/70 (profile + PreferenceManager), msb 8/29, fiapgames 25/9, restapi 0/9,
baseapi 5/3.

| Repo | Scan before (fixed/manual) | Scan after | After mode (fix → build → tests) |
| --- | --- | --- | --- |
| culinary | 0 / 46 | 2 / 70 | ok, 149/149 = base, 0 regressions |
| msb | 3 / 32 | 8 / 29 | ok, 156/156 = base, 0 regressions |
| fiapgames | 25 / 9 | 25 / 9 | ok, 91/91 = base, 0 regressions |
| restapi | 0 / 9 | 0 / 9 | ok, 45/45 = base, 0 regressions |
| baseapi | 5 / 3 | 5 / 3 | ok, 0/9 both (fail in this environment), 0 regressions |
| 14 AutoMapper corpus repos | eshoponweb 4/0, others 0 Fixed | identical findings | not rerun: same rewrites as the earlier rounds |

The AutoMapper corpus scan first showed 105 internal errors in kavita: `GetDeclaredSymbol` throws on a C# 14 `extension` block.
Fixed (`MapOwners.TypeOf`, unit test); kavita rescanned: findings identical to before. Culinary's Manual count grows because its
profile is now rewritten and `automapper.kept-map` reports each kept `CreateMap`. Results: `<corpus>/t279/{blind,am}/_results`.

## Changed files

- `docs/projectinfo/features/automapper-mocked-imapper-plan.md` (plan: option A chosen, option B rejected by measurement)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs`: shared pairs (`_shared`, `Shared`, `ShareCall`, `Share`, closure in `Close`),
  classes under test (`ClassesUnderTest`, `MockVariable`, `CloseUnderTest`, `UnderTestReason`)
- `CreateMapPattern.cs` (a shared statement stays next to its generated methods), `KeptMapPattern.cs` (shared reason)
- `src/Unshackle.Tests/Core/MapOwnersTests.cs`: 4 tests (shared, no construction, IMapper handed on, reached maps)
- Proofs `_Combined/partial-mocked-class-under-test` (stubbed class keeps `IMapper`, other class migrates), `MapCall/mocked-class-under-test` (manual);
  `Helpers/TestReferences.cs`: NSubstitute in the AutoMapper references so a proof can stub `IMapper`
- Docs: `features/automapper-mocked-imapper-modellen.md`, REQs in plans of map-call (15), kept-map (4), create-map (11), partial-migration REQ-8 note, README limitation, INDEX, docs README
- `docs/projectinfo/features/automapper-mocked-imapper-verify.md`; pattern verify notes (map-call, kept-map, create-map)
- Mock set-up reason now says the class under test keeps calling `IMapper` for the stubbed maps (no longer "and so do the maps it stubs")

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/archief/TAAK-272-automapper-partial-migration.md`, `claude/tasks/archief/TAAK-273-automapper-specific-blockers.md`
