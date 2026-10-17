---
kingmadoc: 1
feature: "automapper-convert-using"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/Converters.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberValues.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMaps.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMap.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.convert-using: CreateMap<A, B>().ConvertUsing(...) → B ToB(this A source) => converter call; member-level IValueConverter → X = converter.Convert(source.Y, null)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-253.

## One-sentence summary

automapper.convert-using: a type map with `ConvertUsing` (lambda, `ITypeConverter<A, B>` by type or instance) becomes a `ToB` method that returns what the converter returns, a `ForMember(..., o => o.ConvertUsing(converter[, s => s.Y]))` with an `IValueConverter` becomes `X = converter.Convert(source.Y, null)`, and other maps' members of the converted pair call the new `ToB` without a null check, as AutoMapper 14 does.

## Scope (in / out)

**In scope**

- `CreateMap<A, B>([MemberList]).ConvertUsing(s => expr)`, `ConvertUsing((s, d) => expr)`, `ConvertUsing((s, d, ctx) => expr)` with an expression body that does not use `d` or `ctx`.
- `ConvertUsing<TConverter>()` and `ConvertUsing(new TConverter())` / `ConvertUsing(Type.StaticInstance)` with `ITypeConverter<A, B>` whose `Convert` is implemented implicitly in source of the same project and does not read the destination or the `ResolutionContext`; the generic form only for a class with a parameterless constructor and no other.
- `ForMember(d => d.X, o => o.ConvertUsing(converter[, s => expr]))` and `o.ConvertUsing<TValueConverter, TSourceMember>([s => expr])` with `IValueConverter<TSourceMember, TMember>` under the same conditions; without a source expression the same-named source member of type `TSourceMember`.
- Members and collection elements of other maps whose type pair is a converter map (via the nested-map index, TAAK-245).

**Out of scope (Manual)**

- A converter that reads the destination or the `ResolutionContext`, one with constructor parameters (a service provider creates it), an explicit interface implementation, a `Convert` outside the project, an instance that is not `new C()` or a static field/property, block-bodied lambdas, a string member name.
- `ConvertUsing` together with `ForMember` on the same map; a converter map a `Map(source, destination)` call uses (`MapInto`).
- A source type that already has a member with the method name (`CreateMap<A, string>` → `ToString`).
- `ConvertUsing` in a `MapperConfiguration`/`AddAutoMapper` lambda (outside a profile): still Manual as in TAAK-247 (`CreateMap/conversion-convert-using`).

## Design

Like nested maps and conversions this is a capability of the shared profile rewrite, not a pattern class. `CreateMapPattern.TryParseMap` accepts a trailing `.ConvertUsing(...)` and `ForMember` options with `o.ConvertUsing(...)` (`Converters.MemberOption`). `Converters.TypeProblem` turns the type-level call into the `ToB` body (`Extensions` writes `=> body;`), `Converters.MemberProblem` turns a member converter into the assigned value. Converter calls are checked by speculative binding (with `default(T)` for the source); when several `Convert` overloads make `null` ambiguous, the destination is passed as `default(B)`. `NestedMaps` marks a map as `NestedMap.Converts` (a `ConvertUsing` chained to the `CreateMap`, or a rewritten `To*` method whose body is not `new()`); `MemberValues.Nested` then calls it as `source.X.ToB()` (`!` for a maybe-null member in a nullable context) and skips the cycle check. `MethodName` names keyword destinations by their .NET name (`ToInt32`, `ToString`).

## Requirements

- **REQ-1**: WHEN a profile has `CreateMap<A, B>().ConvertUsing(lambda)` with an expression body THE SYSTEM SHALL write `B ToB(this A source) => <body with the source parameter renamed>` (proof `CreateMap/convert-using-lambda`).
- **REQ-2**: WHEN the map uses `ConvertUsing<TConverter>()` or `ConvertUsing(new TConverter())` with an `ITypeConverter<A, B>` THE SYSTEM SHALL write `=> new TConverter().Convert(source, null, null)` (`default` for a value-type destination, `null!` in a nullable context) (proof `CreateMap/convert-using-type-converter`, unit test `Rewrite_ConverterInNullableContext_CallsItWithoutNullCheck`).
- **REQ-3**: WHEN `Convert` has several overloads THE SYSTEM SHALL pass the destination as `default(B)` so the call binds to the map's implementation (unit test `Rewrite_ConverterWithSeveralOverloads_PassesATypedDestination`).
- **REQ-4**: WHEN a member is configured with `o.ConvertUsing(converter[, s => expr])` or `o.ConvertUsing<TValueConverter, TSourceMember>()` THE SYSTEM SHALL assign `converter.Convert(source.Y, null)` (proof `CreateMap/convert-using-member`).
- **REQ-5**: WHEN another map has a member or element of a converter map's pair THE SYSTEM SHALL call `source.X.ToB()` without `?.` (proofs `CreateMap/convert-using-lambda`, `CreateMap/convert-using-type-converter`, unit test `Rewrite_ConverterInNullableContext_CallsItWithoutNullCheck`).
- **REQ-6**: WHEN a `Map<B>(a)` call uses a converter map THE SYSTEM SHALL rewrite it to `a.ToB()` (proof `_Combined/convert-using-map-call`).
- **REQ-7**: IF the converter reads the destination or the `ResolutionContext`, has constructor parameters, implements `Convert` explicitly, is not a repeatable instance, has a block body, names its member with a string, or is combined with `ForMember` THEN THE SYSTEM SHALL report Manual (proofs `CreateMap/convert-using-context`, `CreateMap/convert-using-service`, unit test `Rewrite_ConvertUsingNotWritable_ReportsManual`).
- **REQ-8**: IF the method name is already a member of the source type THEN THE SYSTEM SHALL report Manual (unit test `Rewrite_ConvertUsingNotWritable_ReportsManual`, `A, string`).
- **REQ-9** (TAAK-273): WHEN the destination is `string` THE SYSTEM SHALL name the method `ToMappedString` instead of `ToString` (every type already has `object.ToString()`, so `x.ToString()` would never call the extension); `Map<string>(a)` calls and nested members use the same name (proof `_Combined/convert-using-string`).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-253 **Evidence**): every converter (lambda, `ITypeConverter`, `IValueConverter`) is called with a null source, also for a member and a collection element; a `Nullable<T>` member that is null maps to null without a call; an exception in a converter is not caught (wrapped in `AutoMapperMappingException` inside a member map); a generic `ITypeConverter` is created per `Map` call, an instance once.

## Risks

- `new C()` per call where AutoMapper reuses one instance differs only for a converter with state.
- A multi-line lambda body is re-indented relative to the line it starts on.

## Models

Class diagram, sequence and decision tree: [automapper-convert-using-modellen.md](automapper-convert-using-modellen.md).

## Open questions

- None.

## See also

- [automapper-convert-using-verify.md](automapper-convert-using-verify.md)
- [automapper-nested-maps-plan.md](automapper-nested-maps-plan.md)
- [automapper-conversions-plan.md](automapper-conversions-plan.md)
- `claude/tasks/archief/TAAK-253-automapper-convert-using.md`
