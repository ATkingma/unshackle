# Verification: automapper-profile-shapes

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-profile-shapes-plan.md`](automapper-profile-shapes-plan.md) |
| **Status**    | Verified                                                                                           |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                                 |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, `ForMemberIgnorePattern.cs`, `ForMemberMapFromPattern.cs`, `MapCallPattern.cs`, `NestedMaps.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/` (`nested-*`, `expression-bodied*`, `member-list-nested-profile`, `partial`, `several-constructors`), `ForMemberIgnore/nested-profile`, `ForMemberMapFrom/nested-*`, `_Combined/nested-profile-map-call`, `_Combined/nested-profiles-nested-map`. Unit tests: `src/Unshackle.Tests/Core/ProfileShapesTests.cs`.

| REQ    | Verdict | Evidence                                                                                                                  |
| ------ | ------- | ------------------------------------------------------------------------------------------------------------------------- |
| REQ-1  | Met     | `CreateMap/nested-profile` (jason `LookupDto` with two maps, `TodoItemBriefDto` with `init`), unit test `Find_NestedProfile_ReportsTheOutermostType` |
| REQ-2  | Met     | `CreateMap/expression-bodied`, `CreateMap/expression-bodied-base` (`: base("…")`, comments kept), `CreateMap/member-list-nested-profile` (was Manual), `CreateMap/nested-record`, unit test `Rewrite_ExpressionBodiedConstructorWithCrLf_KeepsTheLineEndings` |
| REQ-3  | Met     | `CreateMap/nested-record` (cats `TenantDto`), `CreateMap/nested-in-nested` (cats `AddNote.Command` → `this AddNote.Command source`) |
| REQ-4  | Met     | `ForMemberIgnore/nested-profile` (cats `ActivitySummaryDto`), `ForMemberMapFrom/nested-profile` (jason `TodoItemDto`)         |
| REQ-5  | Met     | `_Combined/nested-profile-map-call` (neozhu `AddEditCustomerCommand`: `Map<B>` twice and `Map(request, item)`), unit test `FixAsync_NestedProfileInOtherFile_MapCallsFindItsMethods` |
| REQ-6  | Met     | `_Combined/nested-profiles-nested-map` (jason `TodoListDto.Items` → `TodoItemDto`), unit test `Rewrite_NestedMapOfNestedProfileRewrittenEarlier_CallsItsMethod` |
| REQ-7  | Met     | `CreateMap/nested-private-dto`                                                                                            |
| REQ-8  | Met     | `ForMemberMapFrom/nested-private-member`, `ForMemberMapFrom/nested-outer-member`                                          |
| REQ-9  | Met     | `CreateMap/nested-several-profiles` (cats `AddPRI` form), `CreateMap/nested-name-taken`                                    |
| REQ-10 | Met     | `GenericProfile/skoruba-shape` (skoruba form, automapper.generic-profile since A14), `CreateMap/partial` (nop form), `CreateMap/several-constructors` (dasblog form)          |

## Deviations

- No pattern class of its own: like nested maps and member lists, the shapes are part of the shared profile rewrite; the queue ID names the docs slug.
- `OutsideProblem` also runs for top-level profiles: a `MapFrom` that uses a private or protected member (for example a protected static member of a base class, or a private static member of another class the profile is nested in) was moved into the extension class before and did not compile; it is Manual now. No existing proof changed.
- A finding of a nested profile is reported at the line of the outermost type.
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there. `claude/tools/proofs-doc.py` not run (`proofs.md` is updated by the main session).

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1201/1201 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-profile-shapes-plan.md](automapper-profile-shapes-plan.md)
- [automapper-profile-shapes-modellen.md](automapper-profile-shapes-modellen.md)
- `claude/tasks/archief/TAAK-248-automapper-profile-shapes.md`
