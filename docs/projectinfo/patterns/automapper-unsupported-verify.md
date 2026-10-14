# Verification: automapper-unsupported

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-unsupported-plan.md`](automapper-unsupported-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnsupportedOptions.cs`, hooks in `CreateMapPattern.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/`. Unit tests: `src/Unshackle.Tests/Core/UnsupportedOptionsTests.cs`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `CreateMap/max-depth`, `Rewrite_OptionWithoutEffect_SameAsWithout` (`MaxDepth(0)`, `MaxDepth(1)`, combined) |
| REQ-2 | Met | `CreateMap/preserve-references`, unit test |
| REQ-3 | Met | `CreateMap/for-source-member-do-not-validate`, unit test |
| REQ-4 | Met | `CreateMap/max-depth-nested`, `Rewrite_UnsupportedOption_NamesTheApi` (nested list, `ReverseMap`, non-constant) |
| REQ-5 | Met | `after-map`, `before-map`, `for-all-members`, `construct-using`, `for-ctor-param`, `reverse-map-options`; unit tests for `Include`, `IncludeBase`, `IncludeAllDerived` |
| REQ-6 | Met | `ForMemberIgnore/string-name`, `ForMemberIgnore/string-name` only (Condition, ForPath, ConvertUsing and resolver MapFrom are supported on main since TAAK-253/254/255); unit test `NullSubstitute` |
| REQ-7 | Met | `profile-setting`; unit tests `ShouldMapProperty`, `SourceMemberNamingConvention`, `RecognizePrefixes` |

## Deviations

- `AfterMap` is not rewritten (plan, Out of scope): Manual with the block-bodied form spelled out.
- No rendered figures (no `kingmadoc`/`d2`).

## Validation results

| Check   | Command | Result |
| ------- | ------- | ------ |
| Build   | `dotnet build` | passed (0 warnings) |
| Tests   | `dotnet test` | 1407/1407 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |

## See also

- [automapper-unsupported-plan.md](automapper-unsupported-plan.md)
- [automapper-unsupported-modellen.md](automapper-unsupported-modellen.md)
- `claude/tasks/archief/TAAK-257-automapper-unsupported.md`
