# Verification: automapper-conversions

|               |                                                                                            |
| ------------- | ------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-conversions-plan.md`](automapper-conversions-plan.md) |
| **Status**    | Verified                                                                                   |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                         |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberConversions.cs`, `MemberValues.cs` (`Conversion`), `NestedMaps.cs` (`ConfiguredAt`). Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/conversion*`, `ForMemberMapFrom/conversion*`, `_Combined/conversion-map-into`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ    | Verdict | Evidence                                                                                                       |
| ------ | ------- | -------------------------------------------------------------------------------------------------------------- |
| REQ-1  | Met     | `CreateMap/conversion-nullable` (`int?`, `Guid?`, `DateTime?`, enum? → `T`; `T` → `T?`)                          |
| REQ-2  | Met     | `CreateMap/conversion`, `CreateMap/conversion-to-string` (value types, `DateTime?`, `Uri?` with `!`, enum?), unit test `Rewrite_ConversionToNonNullableString_SuppressesTheWarning` |
| REQ-3  | Met     | `CreateMap/conversion-numbers`, unit tests `Rewrite_BuiltInConversion_WritesIt` (`int` → `char`, `char` → `int`, `int` → `long?`) |
| REQ-4  | Met     | `CreateMap/conversion-enum` (cast `Status` → `StatusDto`, switch `Priority` → `PriorityDto` with case-insensitive names) |
| REQ-5  | Met     | `CreateMap/conversion-enum` (`Code`, `Kind`, `Previous`)                                                        |
| REQ-6  | Met     | `CreateMap/conversion-from-string` (`int`, `decimal`, `bool`, `DateTime`, enum; `using System` added)          |
| REQ-7  | Met     | `CreateMap/conversion-numbers` (`int?` → `long`, `long?` → `int`), `CreateMap/conversion-enum` (`Status?` → `int?`, `Status` → `StatusDto?`), unit tests `Rewrite_BuiltInConversion_WritesIt` |
| REQ-8  | Met     | `ForMemberMapFrom/conversion`, `ForMemberMapFrom/conversion-expression`, `CreateMap/conversion-collection`, `_Combined/conversion-map-into` |
| REQ-9  | Met     | `CreateMap/conversion-enum-member`, `conversion-enum-number`, `conversion-string-nullable`, `conversion-string-guid`, `conversion-nullable-narrowing`, `conversion-nullable-enum-switch`, unit tests `Rewrite_BuiltInConversionNotWritable_ReportsManual` (13 cases) |
| REQ-10 | Met     | `CreateMap/conversion-convert-using`, unit test `Rewrite_ConversionWithNullSetting_ReportsManual`               |
| REQ-11 | Met     | unit test `Rewrite_ConversionOfMapFromCall_OnlyWhenReadOnce`, `CreateMap/conversion-operator`                   |

Runtime check beyond the proofs: the generated `ToB` methods of all Fixed `conversion*` proofs, compiled next to their original profiles, gave the same JSON as `mapper.Map` with AutoMapper 14.0.0 for 216 generated inputs (nulls, edge values, overflow; invariant and nl-NL); see TAAK-247 **Evidence**.

## Deviations

- No pattern class of its own: the behaviour is part of the shared profile rewrite (see plan, Design); the proofs live in the folders of the profile patterns.
- `CreateMap/conversion` and `ForMemberMapFrom/conversion` changed from `.manual.txt` to `.after.cs` (the new output is AutoMapper's measured behaviour).
- The non-generic `Enum.Parse` fallback for target frameworks without `Enum.Parse<T>` has no test (the test references are .NET 10 only).
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1229/1229 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-conversions-plan.md](automapper-conversions-plan.md)
- [automapper-conversions-modellen.md](automapper-conversions-modellen.md)
- `claude/tasks/archief/TAAK-247-automapper-conversions.md`
