# Verification: automapper-generic-map

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-generic-map-plan.md`](automapper-generic-map-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-26 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

> Verified by an AI agent against branch `taak-324`.

## Requirements

Code: `src/Unshackle.Core/Generics/`, `src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericMapPattern.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/GenericMap/`. Unit tests: `src/Unshackle.Tests/Core/AutoMapper/GenericMapPatternTests.cs`.

| REQ | Verdict | Evidence |
| --- | ------- | -------- |
| REQ-1 | Met | `two-callers`, `FixAsync_ProfileAndTwoCallers_DispatchesToGeneratedMethods` (with the profile rewrite) |
| REQ-2 | Met | `derived-repositories`, `Close_TypeParameterOfClass_CollectsUsesAndDerivedTypes` |
| REQ-3 | Met | `constraint-filter` (chain of two), `FixAsync_ChainDeeperThanLimit_IsOpenSet` |
| REQ-4 | Met | `packable-public-base`, `FixAsync_CallersNotAllKnown_IsOpenSet(public)`, `FixAsync_UnprovableCaller_NamesIt(InternalsVisibleTo)`, `FixAsync_PublicMethodInExecutable_IsNotReachableFromOutside`, `FixAsync_PublicMethodInLibraryWithProjectProperties_FollowsIsPackable(false, true)` |
| REQ-5 | Met | `open-registration` |
| REQ-6 | Met | `reflection-caller` |
| REQ-7 | Met | `FixAsync_CallersNotAllKnown_IsOpenSet(virtual)`, `FixAsync_UnprovableCaller_NamesIt` (method group, dynamic receiver) |
| REQ-8 | Met | `FixAsync_PairWithoutMap_IsOpenSet` |
| REQ-9 | Met | `FixAsync_ClosedTypeInDependentProject_IsOpenSet` |
| REQ-10 | Met | `Proofs/MediatR/GenericRequest/*`, `GenericRequestPatternTests` green |

## Deviations

- The constraint filter cannot drop a member of a set from code that compiles; `constraint-filter` shows a constrained chain, not a dropped member.
- An explicit-type-argument call with a dynamic argument binds statically and counts as a normal caller (its type argument is fixed).
- No PNG/SVG of the models (no d2/kingmadoc).

## Validation results

| Check | Command | Result |
| ----- | ------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test` (TEMP on D:) | 3751/3751 |

## Corpus (8 repos, `scan --max-degree 4`, tool from main 75e869b vs taak-324 4cffb12)

| Repo | main Fixed/Manual | taak-324 Fixed/Manual | generic-map rows | After-build/test |
| --- | --- | --- | --- | --- |
| bitwarden | 0/276 | 0/276 | 7 Manual (R1) | build fails in baseline |
| gnomeshade | not loadable (SDK 10.0.202 missing) | – | – | – |
| lancommander | 0/49 | 0/49 | 1 Manual (R1) | build fails in baseline |
| letterbook | 0/165 | 0/165 | 2 Manual (R1) | 537/616 → 537/616, 0 regressions |
| ombi | not loadable (SDK 8.0.419 missing) | – | – | – |
| personalasst | 0/52 | 0/52 | 1 Manual (R1) | 131/131 → 131/131, 0 regressions |
| skoruba | 0/120 | 0/120 | 45 Manual (generic CreateMap, R1) | 272/272 → 272/272, 0 regressions |
| xipona | 0/47 | 0/47 | 2 Manual (closed type not visible) | 969/1193 → 969/1193, 0 regressions |

Row delta 0: every generic call in these repos lives in a public member of a library project, which R1 (packability guessed without
`IProjectProperties`) keeps open; the reason now names the member (`GenericMapOpenSet`). Wiring `IProjectProperties` into `AutoMapperFix`
(`Cli.cs`) is the next lever.

## Corpus with `IProjectProperties` (TAAK-332, tool from main 7c0db29, after-fix round)

`fix --fix automapper --force`, rebuild, test, compare per test against the Windows baseline (2026-10-14). cats and kavita were not
measured (the data disk ran full during restore); bitwarden, gnomeshade, lancommander and ombi have no baseline.

| Repo | Fixed / Manual | generic-map rows | Blocker named by the generic-map rows | tests before → after (pass/total) | regressions |
| --- | --- | --- | --- | --- | ---: |
| letterbook | 0 / 165 | 2 Manual | `Client.Fetch` public in `Letterbook.Adapter.ActivityPub`, packable by SDK default (R1) | 537/616 → 537/616 | 0 |
| personalasst | 0 / 52 | 1 Manual | `UserService.Get` public in `Core.Application`, packable by SDK default (R1) | 131/131 → 131/131 | 0 |
| skoruba | 0 / 120 | 45 Manual | generic profile `CreateMap<TRole, TRole>` (`IdentityRepository.cs:164`), outside A11 | 272/272 → 272/272 | 0 |
| xipona | 0 / 47 | 2 Manual | closed type argument not visible from `Xipona.Api.Core.Tests` | 969/1193 → 969/1193 | 0 |
| jhipster | 0 / 126 | 1 Manual | source is `object` (destination only known at run time) | 146/146 → 146/146 | 0 |
| nop | 0 / 5 | 1 Manual | source is `object` | 1051/1060 → 1051/1060 | 0 |
| altinnauth, devarch, jason, neozhu | 0 / 14, 4, 13, 64 | – | – | unchanged | 0 |
| eshoponweb | 7 / 0 | – | – | 74/74 → 74/74 | 0 |

Manual→Fixed: 0. The wiring works (packability is now read from the project, not guessed), but no repo sets `IsPackable=false`
on its libraries, so the SDK default (`true` for class libraries) keeps R1 open. 0 test regressions over 12 repos.

## See also

- [automapper-generic-map-plan.md](automapper-generic-map-plan.md)
- [automapper-generic-map-modellen.md](automapper-generic-map-modellen.md)
