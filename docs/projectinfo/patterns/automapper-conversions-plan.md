---
kingmadoc: 1
feature: "automapper-conversions"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberConversions.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberValues.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMaps.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.conversions: AutoMapper's built-in member conversions written by hand where the result is the same

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-247.

## One-sentence summary

automapper.conversions: in the `ToB` method that the profile rewrite writes, a member whose type differs from the destination's and has no map gets AutoMapper 14's built-in conversion as a hand-written expression (`GetValueOrDefault()`, `ToString()`, `Convert.ToInt32(...)`, an enum cast or switch, `Enum.Parse`) when that expression gives the same result for every measured value and culture; everything else stays Manual with a precise reason.

## Scope (in / out)

**In scope**

- Same-named members, `MapFrom` expressions and elements of the collections TAAK-245 copies, in profiles one of the three profile patterns rewrites.
- `T?` ↔ `T`; to `string`; numbers, `bool` and `char` among each other; enum ↔ enum; enum ↔ its underlying type; `string` → number/`bool`/`DateTime`/enum; `DateTime` → `DateTimeOffset`; the nullable variants whose null behaviour matches.

**Out of scope (Manual)**

- Conversions AutoMapper 14 does differently from the plain hand-written form: `[EnumMember]` enums, `string` → `T?`, `string` → `Guid`/`TimeSpan`/`char`/`DateOnly`/…, narrowing `T?` → `U?`, a renaming enum switch over a nullable source.
- Conversions AutoMapper 14 throws on at run time: enum ↔ a number type other than its underlying type, `char` ↔ `bool`/`float`/`double`/`decimal`, `DateTimeOffset` → `DateTime`, `DateTime` ↔ `DateOnly`.
- Own types with conversion operators or constructors (strongly typed ids, value objects), type parameters, `dynamic`.
- A pair with a `CreateMap` anywhere in the solution (`ConvertUsing`), `AllowNullCollections`/`AllowNullDestinationValues` set; `.ReverseMap()` profiles (row 10).

## Design

Conversions are part of the shared profile rewrite (`CreateMapPattern` → `MemberValues`), like nested maps: no pattern class. `MemberValues.Problem` first looks for a nested map (TAAK-245); when there is none and the types differ, `MemberValues.Conversion` checks `NestedMaps.ConfiguredAt` (every `CreateMap<A, B>` pair of the solution, also outside profiles) and the null settings, then asks `MemberConversions.Problem`. That class strips `Nullable<T>` from both sides, finds the core conversion (`CoreProblem`, `EnumProblem`, `ToStringProblem`, `FromStringProblem`) as a text form over the operand, and lifts it: `T?` → `U` through `GetValueOrDefault()` only when the default converts to the default, `T?` → `U?` only with a lifted C# conversion of the same result. It reports whether the expression already has the destination type, so a `Select` over collection elements can cast an implicit form (`x => (long)x`). `System` is added to the required usings for `Convert`/`Enum`.

## Requirements

- **REQ-1**: WHEN a member is `T?` in the source and `T` in the destination THE SYSTEM SHALL assign `source.X.GetValueOrDefault()`, and `source.X` for `T` → `T?` (proof `CreateMap/conversion-nullable`).
- **REQ-2**: WHEN the destination is `string` THE SYSTEM SHALL assign `source.X.ToString()` for a non-nullable value type and `source.X?.ToString()` otherwise, with `!` for a non-nullable `string` in a nullable-enabled context (proofs `CreateMap/conversion`, `CreateMap/conversion-to-string`, unit test `Rewrite_ConversionToNonNullableString_SuppressesTheWarning`).
- **REQ-3**: WHEN both types are numbers, `bool` or `char` THE SYSTEM SHALL assign `source.X` for an implicit numeric conversion and `Convert.ToX(source.X)` otherwise (proof `CreateMap/conversion-numbers`, unit tests `Rewrite_BuiltInConversion_WritesIt`).
- **REQ-4**: WHEN both types are enums with the same underlying type THE SYSTEM SHALL assign a cast when every source name keeps its number in the destination, and otherwise a switch with an arm per name that lands on another number and the cast as fallback (proof `CreateMap/conversion-enum`).
- **REQ-5**: WHEN an enum converts to or from its underlying type THE SYSTEM SHALL assign a cast (proof `CreateMap/conversion-enum`).
- **REQ-6**: WHEN the source is `string` and the destination a number, `bool` or `DateTime` THE SYSTEM SHALL assign `Convert.ToX(source.X)`, and for an enum `string.IsNullOrEmpty(source.X) ? default : Enum.Parse<E>(source.X, true)` (proof `CreateMap/conversion-from-string`).
- **REQ-7**: WHEN only one side is nullable THE SYSTEM SHALL lift the conversion only where AutoMapper's null behaviour is kept (`Convert.ToInt32(source.X.GetValueOrDefault())`, `(int?)source.X`) (proofs `CreateMap/conversion-numbers`, `CreateMap/conversion-enum`, unit tests `Rewrite_BuiltInConversion_WritesIt`).
- **REQ-8**: WHEN a `MapFrom` expression or a collection element needs a conversion THE SYSTEM SHALL apply REQ-1 to REQ-7 to it (proofs `ForMemberMapFrom/conversion`, `ForMemberMapFrom/conversion-expression`, `CreateMap/conversion-collection`), and a map that also needs `MapInto` gets the same conversions there (proof `_Combined/conversion-map-into`).
- **REQ-9**: IF AutoMapper 14 converts differently from the hand-written form or throws at run time THEN THE SYSTEM SHALL report Manual with the reason (proofs `CreateMap/conversion-enum-member`, `conversion-enum-number`, `conversion-string-nullable`, `conversion-string-guid`, `conversion-nullable-narrowing`, `conversion-nullable-enum-switch`, unit tests `Rewrite_BuiltInConversionNotWritable_ReportsManual`).
- **REQ-10**: IF a `CreateMap` of the pair exists anywhere in the solution or a null setting is assigned THEN THE SYSTEM SHALL report Manual (proof `CreateMap/conversion-convert-using`, unit test `Rewrite_ConversionWithNullSetting_ReportsManual`).
- **REQ-11**: IF a form would read a `MapFrom` expression with a possible side effect twice, or no built-in conversion is known THEN THE SYSTEM SHALL report Manual (unit test `Rewrite_ConversionOfMapFromCall_OnlyWhenReadOnce`, proof `CreateMap/conversion-operator`).

## Assumptions

- Measured with AutoMapper 14.0.0 in the invariant culture and nl-NL (TAAK-247 **Evidence**): AutoMapper's ToString and Convert paths use the current culture, so do `ToString()` and `Convert.ToX(string)`. Mapper order: ToString before Convert before the enum mappers before conversion operators.
- The generated `ToB` runs with the same current culture as the `Map` call it replaces.

## Risks

- `Convert.ToX` and the enum switch are longer than a cast; a reader may "simplify" them to a cast and change rounding/overflow or the enum mapping. The forms are what AutoMapper did, so behaviour is kept.
- The `!` on `?.ToString()` hides that a non-nullable string can get null; AutoMapper did the same.

## Models

Class diagram, sequence and decision tree: [automapper-conversions-modellen.md](automapper-conversions-modellen.md).

## Open questions

- Own value types with conversion operators (strongly typed ids) stay Manual; rows 19/20 (`ConvertUsing`, value resolvers) may cover them.
