# Verification: automapper-generic-destination

|               |                                                                                                              |
| ------------- | ------------------------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-generic-destination-plan.md`](automapper-generic-destination-plan.md) |
| **Status**    | Verified                                                                                                     |
| **Generated** | 2026-10-27 by KingmaDoc skill (by hand, `kingmadoc` not available)                                           |

> Verified by an AI agent against branch `taak-325`.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericDestinationPattern.cs`, `ProjectToPattern.ProjectionAsync`. Proofs:
`src/Unshackle.Tests/Proofs/AutoMapper/GenericDestination/`. Unit tests: `src/Unshackle.Tests/Core/AutoMapper/GenericDestinationPatternTests.cs`,
`GenericDestinationTranslationTests.cs`.

| REQ | Verdict | Evidence |
| --- | ------- | -------- |
| REQ-1 | Met | `two-callers`, `FixAsync_ProfileWithPlainProjections_SelectsGeneratedProjectionByType` (with the profile rewrite) |
| REQ-2 | Met | `paging-helper` (cats shape, extension-method callers) |
| REQ-3 | Met | `ToQueryString_GenericSelector_EqualsClosedProjection` (EF Core 10.0.13, SQLite in-memory), `Page_TypeOutsideClosedSet_Throws` |
| REQ-4 | Met | `packable-paging-helper` (moved from `ProjectTo/generic`), `FixAsync_PublicInPackableLibrary_IsOpenSet` |
| REQ-5 | Met | `no-projection`, `FixAsync_PairWithoutPlainProjectTo_IsOpenSet` |
| REQ-6 | Met | `parameters` (moved from `ProjectTo/parameters-generic`) |
| REQ-7 | Met | `Find_GenericProjectTo_IsNotProjectToPatterns`, `Proofs/AutoMapper/ProjectTo/*` green |
| REQ-8 | Met | `Proofs/AutoMapper/GenericMap/*`, `Proofs/MediatR/GenericRequest/*`, `GenericMapPatternTests` green |

## Deviations

- Inline conditional selector instead of a private static selector member (same evaluation order; see TAAK-325 council questions).
- The translation test mirrors the emitted shape in compiled test code; it does not compile the proof's `after` against EF Core.
- No PNG/SVG of the models (no d2/kingmadoc).

## Validation results

| Check | Command | Result |
| ----- | ------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test` (TEMP on D:) | 3847/3847 |

## Corpus

Not re-measured yet (TAAK-325 step 6: cats, kavita, neozhu).

## See also

- [automapper-generic-destination-plan.md](automapper-generic-destination-plan.md)
- [automapper-generic-destination-modellen.md](automapper-generic-destination-modellen.md)
