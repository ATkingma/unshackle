# Verification: automapper-constructor

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-constructor-plan.md`](automapper-constructor-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/` (`constructor*`, `construct-using`, `for-ctor-param`), `ForMemberIgnore/constructor`, `ForMemberMapFrom/constructor`, `_Combined/constructor-map-call`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `CreateMap/constructor`, `CreateMap/constructor-record` (altinnauth record shape), unit test `Rewrite_ConstructorParameterOtherCase_PassesMember` |
| REQ-2 | Met     | `ForMemberIgnore/constructor` (`List<Line>` → `List<LineDto>` argument through `ToLineDto`) |
| REQ-3 | Met     | `ForMemberIgnore/constructor` (get-only `Id`/`Lines`, settable `Customer` filled by the constructor only, `init` `Note` assigned, ignored `Status` left alone) |
| REQ-4 | Met     | `CreateMap/constructor-several`, unit test case "private B() + public B(int id)" |
| REQ-5 | Met     | `CreateMap/constructor-default`, `ForMemberMapFrom/constructor`, unit test cases "matches several members", "ref, out or params" |
| REQ-6 | Met     | `CreateMap/construct-using`, `CreateMap/for-ctor-param` |
| REQ-7 | Met     | `_Combined/constructor-map-call`; the `MapInto` guard in `CreateMapPattern.Rewrite` |

## Deviations

- PNG/SVG of the model not rendered (no `kingmadoc`/`d2`); the D2 source is there.

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1278/1278 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-constructor-plan.md](automapper-constructor-plan.md)
- [automapper-constructor-modellen.md](automapper-constructor-modellen.md)
- `claude/tasks/archief/TAAK-251-automapper-constructor.md`
