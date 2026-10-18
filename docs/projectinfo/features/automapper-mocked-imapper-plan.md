---
kingmadoc: 1
feature: "automapper-mocked-imapper"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/KeptMapPattern.cs
    - src/Unshackle.Tests/Core/MapOwnersTests.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: AutoMapper: a mocked IMapper only keeps its class under test

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-18 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-279, on top of [automapper-partial-migration](automapper-partial-migration-plan.md) (TAAK-272, REQ-8 there).

## One-sentence summary

A map pair that a test stubs on a mocked `IMapper` keeps its `CreateMap` for the class under test **and** gets its `ToB()`, so every
call site of that pair outside the class under test is migrated, while the class under test and its test stay exactly as they were.

## Measurement (TAAK-279 step 1)

| Repo                                               | Classes under test (constructor gets the mock)                                | Call sites of stubbed pairs elsewhere                    |
| -------------------------------------------------- | ----------------------------------------------------------------------------- | -------------------------------------------------------- |
| culinary                                           | 5 managers, 21 Map calls                                                      | PreferenceManager (1)                                    |
| msb                                                | 5 handlers/controllers, 9 Map calls (1 more test: construction commented out) | OrderController (1, and 1 that waits on it)              |
| dasblog                                            | BlogPostViewModelCreator (1)                                                  | BlogPostController (1; profile stays for another reason) |
| fiapgames, restapi, baseapi, 13 other corpus repos | no mocked `IMapper`                                                           | —                                                        |

## Design decision: option A (both owners), not option B (rewrite the test)

- **Option B** (remove the stub, let the real mapping run) changes what the test checks. Measured: culinary asserts
  `Assert.Equal(expectedAccount, result.Value)` on a canned DTO whose `AccountId` the source (`new Account()`) does not have, and
  verifies `Map` calls (`Verify(m => m.Map<…>(…), Times.Once)`, 5×); msb asserts the canned instance (`Assert.Equal(warehouseDto, …)`).
  A real mapping fails these or makes them meaningless → not provably the same → rejected.
- **Option A**: the stubbed pair gets two owners. The class under test keeps calling `IMapper.Map<B>` (its test still stubs it, and in
  the application AutoMapper still has the `CreateMap`); every other class calls `ToB()`. `ToB()` is equivalent to AutoMapper's map by the
  existing proofs of the profile patterns, so the application behaves the same whichever owner a call site uses.
- "Class under test" is proven by the test, not by a name: the type of a `new C(…)` (or `new(…)` typed `C`) in the test file whose
  argument is the mock (`mock.Object` for Moq, the variable itself for NSubstitute/FakeItEasy), plus the base types of `C` and every
  type `C` hands an `IMapper` to (a constructor or method argument of type `IMapper`), because those calls run the stub too.

## Scope (in / out)

**In scope**

- Mock set-ups and checks of `Map` (Moq `Setup`/`Verify`, NSubstitute `.Returns`/`Received()`, FakeItEasy `A.CallTo`) whose mock
  variable (local, field, property) is passed to a constructor in the same file.
- Shared pairs: `CreateMap` stays in the profile constructor and a `ToB()` is generated next to the migrated maps.

**Out of scope**

- A mock that reaches the class under test through a fixture, a base test class, a DI container or a factory: the pair stays on
  AutoMapper as before (safe).
- Rewriting the tests themselves (option B) and removing `IMapper` from classes that no longer use it (REQ-7 of the partial migration,
  deferred).

## Requirements

- **REQ-1**: WHEN a mock set-up of `IMapper` stubs pair A→B AND its test constructs class C with that mock THE SYSTEM SHALL keep
  `CreateMap<A, B>` in the profile and also generate `ToB()` for it.
- **REQ-2**: WHEN class C (or a base type of C, or a type C hands its `IMapper` to) calls `Map` of a shared pair THE SYSTEM SHALL leave
  the call on `IMapper` and name the class under test in the reason.
- **REQ-3**: WHEN any other class calls `Map` of a shared pair THE SYSTEM SHALL rewrite the call to `ToB()`.
- **REQ-4**: WHEN the class under test of a stub cannot be found in the test file THE SYSTEM SHALL keep the pair on AutoMapper everywhere
  (as before).
- **REQ-5**: WHEN a shared map may map a member through another map THE SYSTEM SHALL keep that `CreateMap` in the profile too (shared);
  a pair kept for another reason stays kept (kept wins).
- **REQ-6**: WHEN the `ToB()` of a shared pair cannot be generated THE SYSTEM SHALL keep the pair on AutoMapper everywhere.
- **REQ-7**: THE SYSTEM SHALL never rewrite a mock set-up (unchanged).
- **REQ-8**: WHILE a shared `CreateMap` stays THE SYSTEM SHALL keep `AddAutoMapper` and the configuration test (unchanged: `IMapper` is
  still used, maps remain).

## Assumptions

- AutoMapper 14 and `ToB()` give the same result for a pair the profile patterns can rewrite (existing proofs and corpus rounds).
- A Map call runs the stub only when its `IMapper` is the mock; the mock reaches code only through the constructor call in the test
  (or what that class passes on), which the closure follows.

## Risks

| Risk                                                                                      | Mitigation                                                                                                                                 |
| ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| The class under test is also used in the application with a real `IMapper`                | It keeps `IMapper` everywhere (owner = class, not call site); AutoMapper still has the map                                                 |
| The mock is passed on through a path the closure does not follow (a property, a delegate) | Only argument passing is followed; corpus `after` with the projects' own tests catches a stub that no longer runs (0 regressions required) |
| Two owners drift apart                                                                    | Both come from the same `CreateMap`; the `CreateMap` is not changed                                                                        |

## Models

[automapper-mocked-imapper-modellen.md](automapper-mocked-imapper-modellen.md).

## Open questions

- None.

## See also

- [verify](automapper-mocked-imapper-verify.md), [automapper-partial-migration](automapper-partial-migration-plan.md)
- `claude/tasks/TAAK-279-automapper-mocked-imapper.md`
