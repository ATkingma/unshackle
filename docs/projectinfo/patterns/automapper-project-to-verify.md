# Verification: automapper-project-to

|               |                                                                                            |
| ------------- | ------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-project-to-plan.md`](automapper-project-to-plan.md) |
| **Status**    | Verified                                                                                   |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                         |

> Verified by an AI agent against the code of TAAK-252.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectToPattern.cs`, `Projections.cs`, `ProjectionMember.cs`, `CreateMapPattern.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ProjectTo/`, `CreateMap/project-to*`, `_Combined/project-to-query`. Unit tests: `src/Unshackle.Tests/Core/ProjectToPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                         |
| ----- | ------- | ---------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | `CreateMap/project-to` (property after the `ToB` methods, usings added)                                           |
| REQ-2 | Met     | `CreateMap/project-to` (`Years = source.Years`)                                                                   |
| REQ-3 | Met     | `CreateMap/project-to` (`null!`, nullable context), `_Combined/project-to-query` (`null`)                          |
| REQ-4 | Met     | `CreateMap/project-to` (array, `ToArray()`), `_Combined/project-to-query` (list, `ToList()`)                       |
| REQ-5 | Met     | `_Combined/project-to-query` (`Summary` from `MapFrom`, `Note` ignored)                                           |
| REQ-6 | Met     | `ProjectTo/existing-projection` (chain, `mapper.ProjectTo<B>(query)`), `_Combined/project-to-query`, unit test `FixAsync_ProfileInOtherFile_WritesProjectionAndSelectsIt` |
| REQ-7 | Met     | `ProjectTo/parameters-generic`, `ProjectTo/generic`, `ProjectTo/unknown-map`, unit test `FixAsync_ParametersThatDoNotFitTheCapturedVariable_AreManual`                                     |
| REQ-8 | Met     | `CreateMap/project-to-conversion` (`int?` → `int`), unit test `FixAsync_NestedMapInOtherProfile_IsManual`          |
| REQ-9 | Met     | unit test `FixAsync_NoProjectToCall_WritesNoProjection`; every earlier AutoMapper proof unchanged                 |
| REQ-10 | Met | `_Combined/projection-parameters` (`ToSeriesDto(this Series source, int userId = 0)`, `SeriesToSeriesDtoProjection(int userId = 0)`, calls with `new { userId }`, `new { userId = owner.Id }`, none; `Map` keeps `ToSeriesDto()`), unit test `FixAsync_CapturedVariable_ProjectionTakesItAsParameter` |
| REQ-11 | Met | `ProjectTo/parameters` (name the map does not read), unit test `FixAsync_ParametersThatDoNotFitTheCapturedVariable_AreManual` (name, type, no anonymous object) |

`dotnet build` (warnings = errors), `dotnet test` (1239 passed) and `dotnet test --solution samples/SampleShop/SampleShop.slnx` (38 passed) green.

## Deviations

- The task suggested removing an `IMapper` that becomes unused; `MapCallPattern` keeps it (removing a constructor parameter changes every caller), so this pattern keeps it too.
- A nested map of another profile is Manual instead of inlined (see plan, Open questions).
