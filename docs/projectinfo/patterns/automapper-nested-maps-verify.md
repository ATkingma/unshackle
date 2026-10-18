# Verification: automapper-nested-maps

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-nested-maps-plan.md`](automapper-nested-maps-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, `MemberValues.cs`, `NestedMaps.cs`, `NestedMap.cs`, `MapCallPattern.cs`, `MapIntoPattern.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/` (`nested*`, `collection*`), `ForMemberMapFrom/` (`nested`, `collection`), `_Combined/` (`nested-*`). Unit tests: `src/Unshackle.Tests/Core/NestedMapsTests.cs`.

| REQ    | Verdict | Evidence                                                                                                                      |
| ------ | ------- | ----------------------------------------------------------------------------------------------------------------------------- |
| REQ-1  | Met     | `CreateMap/nested` (class, struct, `Point?` → `PointDto?`, class → struct with `?? default`)                                   |
| REQ-2  | Met     | `CreateMap/nested-collections` (List, array, IEnumerable, ICollection → IList, IReadOnlyList → IReadOnlyCollection, struct elements) |
| REQ-3  | Met     | `CreateMap/collection`, `ForMemberMapFrom/collection`, `CreateMap/nested-collections` (`List<string>` → `string[]`)             |
| REQ-4  | Met     | `CreateMap/nested-same-type`                                                                                                  |
| REQ-5  | Met     | `CreateMap/nested-other-profile` (using added), `_Combined/nested-profile-rewritten-earlier`, unit tests `Rewrite_NestedMapInOtherFileOfTheProject_IsFoundWithoutTheSolution`, `FixAsync_NestedMapInProjectDependedOn_CallsItsMethod` |
| REQ-6  | Met     | `CreateMap/nested-nullable` (`!` only on the non-nullable member and elements)                                                |
| REQ-7  | Met     | `ForMemberMapFrom/nested`, `ForMemberMapFrom/collection`                                                                      |
| REQ-8  | Met     | `CreateMap/nested-no-map`, `CreateMap/nested-several`, `CreateMap/nested-cycle`, unit test `FixAsync_NestedMapInternalInOtherProject_ReportsManual` |
| REQ-9  | Met     | `CreateMap/collection-dictionary`, unit test `Rewrite_BeforeCSharp12_FallsBackToNewEmptyCollections`                           |
| REQ-10 | Met     | unit test `FixAsync_MapIntoWithNestedMember_ReportsManual`, `_Combined/nested-map-call`                                       |
| REQ-11 | Met | `_Combined/cycle-destination-only` (Fixed), `_Combined/partial-cycle-source` and `CreateMap/nested-cycle` (real cycle stays); corpus msb (TAAK-273) |

## Deviations

- No pattern class of its own: the behaviour is part of the shared profile rewrite (see plan, Design). The proofs live in the folders of `CreateMapPattern` and `ForMemberMapFromPattern`.
- Extra Manual case, not in the queue row: an assignment to `AllowNullCollections`/`AllowNullDestinationValues` anywhere in the solution makes nested and copied members Manual (unit test `Rewrite_AllowNullCollectionsConfigured_ReportsManual`).
- `MapCallPattern.GeneratedAsync` now rewrites profiles with the solution (`CreateMapPattern.RewriteMethodsAsync`); `MapCallPattern`/`MapIntoPattern` lost their unused helper fields.
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1166/1166 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-nested-maps-plan.md](automapper-nested-maps-plan.md)
- [automapper-nested-maps-modellen.md](automapper-nested-maps-modellen.md)
- `claude/tasks/archief/TAAK-245-automapper-nested-maps.md`
