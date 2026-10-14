# Verification: automapper-member-list

|               |                                                                                            |
| ------------- | ------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-member-list-plan.md`](automapper-member-list-plan.md) |
| **Status**    | Verified                                                                                   |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                         |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`TryParseMap`, `IsMemberList`). Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/member-list*`, `ForMemberIgnore/member-list`, `ForMemberMapFrom/member-list`, `_Combined/member-list-map-call`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                       |
| ----- | ------- | -------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | `CreateMap/member-list` (Destination with an extra source member, Source, None), unit test `Rewrite_ConstantMemberList_SameAsWithout` (output equal to `CreateMap<A, B>()`) |
| REQ-2 | Met     | `CreateMap/member-list-named` (`memberList:`, `AutoMapper.MemberList.Source`), unit test `Rewrite_ConstantMemberList_SameAsWithout` (`(MemberList)1`) |
| REQ-3 | Met     | `ForMemberIgnore/member-list`, `ForMemberMapFrom/member-list` (MapFrom + Ignore, named `memberList: MemberList.Source`) |
| REQ-4 | Met     | `_Combined/member-list-map-call`                                                                               |
| REQ-5 | Met     | `CreateMap/member-list-variable` (method call), unit test `Rewrite_MemberListParameter_ReportsManual`           |
| REQ-6 | Met     | `CreateMap/member-list-reverse-map`, `CreateMap/member-list-nested-profile`                                     |

## Deviations

- No pattern class of its own: the behaviour is part of the shared profile rewrite (see plan, Design); the proofs live in the folders of the three profile patterns.
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1180/1180 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-member-list-plan.md](automapper-member-list-plan.md)
- [automapper-member-list-modellen.md](automapper-member-list-modellen.md)
- `claude/tasks/archief/TAAK-246-automapper-member-list.md`
