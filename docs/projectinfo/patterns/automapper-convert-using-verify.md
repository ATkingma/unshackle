# Verification: automapper-convert-using

|               |                                                                                                  |
| ------------- | ------------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-convert-using-plan.md`](automapper-convert-using-plan.md) |
| **Status**    | Verified                                                                                         |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                               |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/Converters.cs`, `CreateMapPattern.cs`, `MemberValues.cs`, `NestedMaps.cs`, `NestedMap.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/convert-using-*`, `_Combined/convert-using-map-call`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                 |
| ----- | ------- | -------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | `CreateMap/convert-using-lambda` (`id => new(id)`, `id => id.Id`, `(s, _) => DateTime.ParseExact(...)`)  |
| REQ-2 | Met     | `CreateMap/convert-using-type-converter` (generic and instance form), unit test `Rewrite_ConverterInNullableContext_CallsItWithoutNullCheck` |
| REQ-3 | Met     | unit test `Rewrite_ConverterWithSeveralOverloads_PassesATypedDestination`                                 |
| REQ-4 | Met     | `CreateMap/convert-using-member` (static instance with source lambda, generic with same-named member)    |
| REQ-5 | Met     | `CreateMap/convert-using-lambda`, `CreateMap/convert-using-type-converter`, unit test `Rewrite_ConverterInNullableContext_CallsItWithoutNullCheck` |
| REQ-6 | Met     | `_Combined/convert-using-map-call`                                                                        |
| REQ-7 | Met     | `CreateMap/convert-using-context`, `CreateMap/convert-using-service`, unit test `Rewrite_ConvertUsingNotWritable_ReportsManual` (11 cases) |
| REQ-8 | Superseded for `string` by REQ-9 | other names: code review (the `CreateMap<A, string>` case of `Rewrite_ConvertUsingNotWritable_ReportsManual` moved to REQ-9)                       |
| REQ-9 | Met | `_Combined/convert-using-string` (`ToMappedString` from the profile, a nested member and `Map<string>`), unit test `Rewrite_ConvertUsingToString_WritesToMappedString` (TAAK-273) |

## Deviations

- No pattern class of its own (as TAAK-245/247): the behaviour is part of the shared profile rewrite; the proofs live in `CreateMap/`.
- `MapMethodWriter.MethodName` now names keyword destinations by their .NET type name (`ToInt32`); before, such a map could not be written.
- The `MapInto` Manual case for a converter map has no test of its own (it needs a solution with a `Map(source, destination)` call).
- No figures (no `kingmadoc`/`d2`).

## Validation results

| Check   | Command                                                     | Result                                 |
| ------- | ----------------------------------------------------------- | -------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1248/1248 passed                       |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed                           |
| Lint    | _none_                                                      | _not run_: no lint command configured  |

## See also

- [automapper-convert-using-plan.md](automapper-convert-using-plan.md)
- [automapper-convert-using-modellen.md](automapper-convert-using-modellen.md)
- `claude/tasks/archief/TAAK-253-automapper-convert-using.md`
