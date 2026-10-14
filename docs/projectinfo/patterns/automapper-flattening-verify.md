# Verification: automapper-flattening

|               |                                                                                          |
| ------------- | ---------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-flattening-plan.md`](automapper-flattening-plan.md) |
| **Status**    | Verified                                                                                 |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                       |

> Verified by an AI agent against the code of the TAAK-256 commit.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/Flattening.cs`, `CreateMapPattern.cs` (`Problem`, `FlattenedProblem`), `MemberValues.cs` (`NullableWarnings`). Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/` (`flattening*`), `_Combined/flattening-map-call`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `CreateMap/flattening-get-method` (`number` → `Number`, `Code()` → `Code`), unit test `Rewrite_OtherNameRule_ReadsWhatAutoMapperReads` (`id` → `Id`) |
| REQ-2 | Met     | `CreateMap/flattening-get-method` (`GetTax()`, `Total` beats `GetTotal()`), unit tests (property `GetCode`, field `Total` beats `GetTotal()`) |
| REQ-3 | Met     | `CreateMap/flattening`, `CreateMap/flattening-multi-level` (three levels, struct `Point.X`, method `GetParent()` on the way), unit test `CustomerName2X` (backtracking) |
| REQ-4 | Met     | `CreateMap/flattening-nullable` (`?? default`, lifted `int?`), `CreateMap/flattening-multi-level` |
| REQ-5 | Met     | `CreateMap/flattening-nullable` (`source.Library?.Name!`) |
| REQ-6 | Met     | `CreateMap/flattening-nested-map`, `CreateMap/flattening-multi-level` (`(… ?? default).ToString()`), unit test `int?` leaf → `GetValueOrDefault()` |
| REQ-7 | Met     | `CreateMap/flattening-case`, `CreateMap/flattening-ambiguous` |
| REQ-8 | Met     | `CreateMap/flattening-nullable-conversion`, unit test `Rewrite_OtherNameRuleUnsure_ReportsManual` (`Nullable<T>` on the way) |
| REQ-9 | Met     | `_Combined/flattening-map-call` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors.
- `dotnet test`: 1319 passed, 0 failed.
- `dotnet test --solution samples/SampleShop/SampleShop.slnx`: 38 passed.

## See also

- [automapper-flattening-plan.md](automapper-flattening-plan.md)
- [automapper-flattening-modellen.md](automapper-flattening-modellen.md)
