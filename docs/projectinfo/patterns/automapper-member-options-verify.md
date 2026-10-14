# Verification: automapper-member-options

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-member-options-plan.md`](automapper-member-options-plan.md) |
| **Status**    | Verified                                                                                           |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                                 |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, `MemberOptions.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/` (`condition*`, `pre-condition`, `for-path*`, `null-substitute`, `reverse-map-options`), `_Combined/condition-map-call`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs` (`Rewrite_MemberOptionAutoMapperDoesMoreFor_ReportsManual`, `Rewrite_ConditionOnConditionalValue_GroupsTheValue`).

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `CreateMap/condition` (cats shape `DocumentId != Guid.Empty`), `_Combined/condition-map-call` |
| REQ-2 | Met     | `CreateMap/condition-map-from` (letterbook block shape), `CreateMap/pre-condition` |
| REQ-3 | Met     | `CreateMap/condition-map-from` (`string?`: `default`), unit test `Rewrite_ConditionOnConditionalValue_GroupsTheValue` |
| REQ-4 | Met     | `CreateMap/condition-initializer` (letterbook `Id = Guid.NewGuid()` shape), unit test case "is created through its constructor" |
| REQ-5 | Met     | `CreateMap/for-path` (personalasst shape), `CreateMap/for-path-member` (cats shape), unit test cases "also maps itself", "not a class with a public parameterless constructor" |
| REQ-6 | Met     | `CreateMap/for-path-deep`, `CreateMap/condition-destination`, `CreateMap/null-substitute`, unit test cases `UseDestinationValue`, four-parameter `Condition` |
| REQ-7 | Met     | `CreateMap/reverse-map-options` (jhipster shape, reason now precise), unit test case "not migrated together with ReverseMap"; the MapInto guard in `CreateMapPattern.Rewrite` |
| REQ-8 | Met     | `_Combined/condition-map-call` |

## Deviations

- PNG/SVG of the model not rendered (no `kingmadoc`/`d2`); the D2 source is there.
- `docs/projectinfo/architectuur/proofs.md` not regenerated in this branch (left to the merge).

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1317/1317 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-member-options-plan.md](automapper-member-options-plan.md)
- [automapper-member-options-modellen.md](automapper-member-options-modellen.md)
- `claude/tasks/archief/TAAK-255-automapper-member-options.md`
