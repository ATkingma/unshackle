---
kingmadoc: 1
feature: "automapper-mocked-imapper"
status: verified
---

# Verify: AutoMapper: a mocked IMapper only keeps its class under test

|              |                                                                        |
| ------------ | ---------------------------------------------------------------------- |
| **Plan**     | [automapper-mocked-imapper-plan.md](automapper-mocked-imapper-plan.md) |
| **Verified** | 2026-10-18 by KingmaDoc (by hand, `kingmadoc` not available)           |

## Requirements

| REQ   | Status        | Evidence                                                                                                                                                                                                                                                                                                                                         |
| ----- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| REQ-1 | met           | `MapOwnersTests.FixAsync_MockedMap_KeepsOnlyTheClassUnderTestOnIMapper`; proof `_Combined/partial-mocked-class-under-test`                                                                                                                                                                                                                       |
| REQ-2 | met (widened) | Every Map call of a class a test constructs with a mocked `IMapper` stays, stubbed or not (culinary `GroceryManager`: Moq mock without set-ups, a loose mock answers null). Tests `FixAsync_MockWithoutSetUp_KeepsItsClassOnIMapper`, `FixAsync_ClassUnderTestHandsItsMapperOn_KeepsThatClassOnIMapper`; proof `MapCall/mocked-class-under-test` |
| REQ-3 | met           | Other classes call `ToB()` (culinary `PreferenceManager`, msb `OrderController` ×2)                                                                                                                                                                                                                                                              |
| REQ-4 | met           | `FixAsync_MockedMapWithoutConstruction_KeepsThePairEverywhere` (msb `AddOrderCommandHandlerTests` has its construction commented out)                                                                                                                                                                                                            |
| REQ-5 | met           | `FixAsync_SharedMap_KeepsTheMapsItReachesInTheProfile`                                                                                                                                                                                                                                                                                           |
| REQ-6 | met           | A shared statement whose rewrite fails becomes a plan problem and is kept (`CreateMapPattern.PlanAsync` → `MapOwners.Keep`)                                                                                                                                                                                                                      |
| REQ-7 | met           | Mock set-ups stay (test text unchanged in the unit test and proof)                                                                                                                                                                                                                                                                               |
| REQ-8 | met           | `AddAutoMapper` and the configuration test stay (`IsEmpty` counts shared pairs)                                                                                                                                                                                                                                                                  |

## Deviation from the plan

- REQ-2 was widened after the first corpus scan: not only the stubbed pairs, but every Map call of a class under test keeps `IMapper`, and
  classes under test are found from any mock of `IMapper` that a test passes to a constructor (not only from set-ups).
- C# 14 `extension` blocks are skipped when looking for the class around a call: `GetDeclaredSymbol` throws on them (kavita scan showed
  internal errors; test `FixAsync_MapCallInAnExtensionBlock_HasNoInternalError`).

## Corpus

Tool before = main `4392e29`, after = this branch. Fixed / Manual from `scan`; after mode = fix → build → own tests.

| Repo                       | Before    | After     | After mode                                   | Regressions |
| -------------------------- | --------- | --------- | -------------------------------------------- | ----------: |
| culinary                   | 0 / 46    | 2 / 70    | 149/149 = base                               |           0 |
| msb                        | 3 / 32    | 8 / 29    | 156/156 = base                               |           0 |
| fiapgames                  | 25 / 9    | 25 / 9    | 91/91 = base                                 |           0 |
| restapi                    | 0 / 9     | 0 / 9     | 45/45 = base                                 |           0 |
| baseapi                    | 5 / 3     | 5 / 3     | 0/9 = base (fail in this environment)        |           0 |
| 14 AutoMapper corpus repos | unchanged | unchanged | same rewrites as before (identical findings) |           0 |

culinary's Manual count grows because its profile is now rewritten: every `CreateMap` that stays is reported once more by
`automapper.kept-map`. The 14 corpus repos have no class under test with other call sites that can migrate (dasblog's profile stays for
another reason); their reports differ only in which `IMapper` use the `AddAutoMapper` reason names and in the reworded mock set-up reason.

## See also

- [plan](automapper-mocked-imapper-plan.md), [models](automapper-mocked-imapper-modellen.md), `claude/tasks/archief/TAAK-279-automapper-mocked-imapper.md`
