# Verification: automapper-partial-migration

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`automapper-partial-migration-plan.md`](automapper-partial-migration-plan.md)                 |
| **Status**    | Verified with deviations (REQ-7 deferred)                                                    |
| **Generated** | 2026-10-17 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

> Verified by an AI agent against branch `worktree-agent-a50ecf5da4db68373` (TAAK-272).

## Requirements

| REQ    | Verdict  | Evidence |
| ------ | -------- | -------- |
| REQ-1  | Met      | `_Combined/partial-statement`, `MapOwnersTests.FixAsync_OneMapWithAProblem_KeepsOnlyThatMapOnAutoMapper` |
| REQ-2  | Met      | `_Combined/partial-closure`, `MapOwnersTests.FixAsync_KeptMap_KeepsTheMapsItIsConnectedWith` (first case) |
| REQ-3  | Met      | same test, second case (kept pairs are left out of `NestedMaps`) |
| REQ-4  | Met      | plan seeds every call the call patterns leave Manual; `MapCallPatternTests.FixAsync_MapNotRewritten_NamesTheBlocker` |
| REQ-5  | Met      | `_Combined/partial-untyped-source`, `MapOwnersTests.FixAsync_UntypedSource_KeepsEveryMapToItsDestination` |
| REQ-6  | Met      | `KeptCallAsync` in MapCall/MapInto/ProjectTo/CreateMap (other calls) |
| REQ-7  | Deferred | no `MapperCleanupPattern`: `AddAutoMapper` and `IMapper` fields stay (kept row, safe) |
| REQ-8  | Met      | `MapOwnersTests.FixAsync_MockedMap_StaysOnAutoMapper` (NSubstitute); Moq/FakeItEasy by assembly name |
| REQ-9  | Met      | `AddAutoMapperPattern` unchanged rule, Manual row is a planned keep |
| REQ-10 | Met      | `ConfigurationTestPattern` keeps checks and test mappers while any map stays |
| REQ-11 | Met      | `MapOwnersTests.FixAsync_UnplannedManual_LeavesTheSolutionAsItIs`; `AutoMapperFix.AllowsPartial` only for `Stays on AutoMapper: ` |

## Corpus (tool from this branch vs. 5b07976, Linux, `<corpus>/t272`)

| Repo | Fixed / Manual before | Fixed / Manual after | Tests before → after | Regressions |
| --- | --- | --- | --- | ---: |
| fiapgames | 0 / 32 | **25 / 9** | 91/91 → 91/91 | 0 |
| baseapi | 0 / 7 | **5 / 3** | 0/9 → 0/9 (all fail in baseline) | 0 |
| msb | 0 / 33 | **1 / 33** | 156/156 → 156/156 | 0 |
| culinary | 0 / 46 | 0 / 46 (all kept: null sources, cycle, `Mock<IMapper>` stubs, closure) | 149/149 → 149/149 | 0 |
| restapi | 0 / 6 | 0 / 9 (all kept; 3 more kept rows) | 45/45 → 45/45 | 0 |
| bookstore | 33 / 0 | 33 / 0 | ok | 0 |
| altinnauth, dasblog, devarch, eshoponweb, jason, jhipster, kavita, letterbook, neozhu, nop, personalasst, skoruba | unchanged (eshoponweb 4 / 0, dasblog 0 / 54 → 0 / 61) | | ok, compare green | 0 |
| xipona | 0 / 47 | 0 / 47 | build after fix fails (CS1061 `AddDomainEventHandlers`, a source generator after bin/obj clean on Linux); the tool changes no file (checked with git on a fresh copy) | — |

## Deviations

- REQ-7 deferred (see above). A test `MapperConfiguration` with its own maps still undoes the whole fix (conservative).
- The closure is over-approximated (every pair whose both types the kept map can meet): safe, smaller gain (culinary).
- The plan cache reuses a plan for a solution that differs only in the file being fixed; a second run where exactly one file was edited by hand in the same process could reuse a stale plan (shortcut noted in `MapOwners.ForAsync`).
- PNG/SVG of the models not rendered; D2 sources are there.

## See also

- [plan](automapper-partial-migration-plan.md), [models](automapper-partial-migration-modellen.md)
