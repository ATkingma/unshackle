---
kingmadoc: 1
feature: "automapper-flattening"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/Flattening.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberValues.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.flattening: a destination member AutoMapper reads under another name → CustomerName = source.Customer?.Name

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Project**   | Unshackle                                                          |
| **Status**    | Approved                                                           |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-256.

## One-sentence summary

automapper.flattening: in the `ToB` method that the profile rewrite writes, a destination member without a source member of its exact name gets the value AutoMapper 14 reads for it (another case, `GetX()`, flattening `Customer.Name` → `CustomerName`, several levels), null-safe like AutoMapper.

## Scope (in / out)

**In scope**

- Same name in another case (`Number` ← `number`), an exact-named parameterless method (`Code` ← `Code()`), the prefix `Get` on a method or property (`Tax` ← `GetTax()`).
- Flattening over any number of levels, through properties, fields and parameterless methods (`CustomerParentAge` ← `Customer.GetParent().Age`), and through structs.
- The flattened value through the existing member values: nested maps (`(source.Customer?.Address)?.ToAddressDto()`) and AutoMapper's built-in conversions (`(source.Customer?.Age ?? default).ToString()`).

**Out of scope (Manual)**

- Two members matching the same name (`Name` and `name`), two complete flattening paths (`Customer.Address.City` and `CustomerAddress.City`).
- A value-type leaf behind a possibly null object going into a `Nullable<U>` of another type (not measured).
- A `Nullable<T>` struct on the way (AutoMapper does not follow it), names without a path.
- Constructor parameters (stay Manual as in TAAK-251) and members of a reverse map (unflattening stays Manual, TAAK-249).

## Design

Part of the shared profile rewrite, like TAAK-245..251. `Flattening.Problem` finds the path AutoMapper reads: a direct match (same name in any case, else `Get` + name; several → Manual), else AutoMapper's PascalCase word split, shortest prefix first, recursing into the member's type; a direct match ends the search at each level. Every complete path is collected; more than one → Manual. `Flattening.Value` writes the path with `?.` after every reference type and decides the value for a value-type leaf (`?? default`, or the plain lifted value for `T?`). `CreateMapPattern.FlattenedProblem` connects both and hands the expression and its type to `MemberValues.Problem`, which applies nested maps and conversions unchanged. `MemberValues.NullableWarnings` tells whether a `!` is needed.

## Requirements

- **REQ-1**: WHEN a destination member has no source member of its exact name but one of the same name in another case or an exact-named parameterless method THE SYSTEM SHALL assign that member or call (proof `CreateMap/flattening-get-method`, unit test `Rewrite_OtherNameRule_ReadsWhatAutoMapperReads`).
- **REQ-2**: WHEN only `GetX` matches THE SYSTEM SHALL read it (method or property), and a member `X` SHALL win over `GetX()` (proof `CreateMap/flattening-get-method`, unit tests).
- **REQ-3**: WHEN the name splits into a path of members THE SYSTEM SHALL assign `source.A?.B?.C` with `?.` after every reference type and `.` after a struct (proofs `CreateMap/flattening`, `CreateMap/flattening-multi-level`).
- **REQ-4**: WHEN the leaf is a value type behind a possibly null object THE SYSTEM SHALL add `?? default`, or keep the lifted value for a `T?` destination (proofs `CreateMap/flattening-nullable`, `CreateMap/flattening-multi-level`).
- **REQ-5**: WHEN the destination is non-nullable in a nullable context and the leaf a reference type THE SYSTEM SHALL add `!` (proof `CreateMap/flattening-nullable`).
- **REQ-6**: WHEN the leaf type differs THE SYSTEM SHALL apply the nested map or built-in conversion to the null-safe value (proofs `CreateMap/flattening-nested-map`, `CreateMap/flattening-multi-level`).
- **REQ-7**: IF two members match one name or two complete paths exist THEN THE SYSTEM SHALL report Manual naming both (proofs `CreateMap/flattening-case`, `CreateMap/flattening-ambiguous`).
- **REQ-8**: IF a value-type leaf behind a null object goes into another `Nullable<U>`, or no path exists THEN THE SYSTEM SHALL report Manual (proof `CreateMap/flattening-nullable-conversion`, unit test `Rewrite_OtherNameRuleUnsure_ReportsManual`).
- **REQ-9**: WHEN a `Map<B>(a)` call uses such a map THE SYSTEM SHALL rewrite it to `a.ToB()` (proof `_Combined/flattening-map-call`).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-256 **Evidence**): direct match before flattening, shortest prefix first with backtracking, `Get` prefix on methods and properties, null intermediate → default of the leaf (also `"0"` for an `int` going into `string`), but `null` for an `int` into `int?`.

## Risks

- The reverse-map check (TAAK-249) keeps flattened forward maps with `ReverseMap()` Manual; this feature does not change that.
- The `!` hides a null AutoMapper also writes.

## Models

Class diagram, sequence and decision tree: [automapper-flattening-modellen.md](automapper-flattening-modellen.md).

## Open questions

- None.

## See also

- [automapper-flattening-verify.md](automapper-flattening-verify.md)
- `claude/tasks/archief/TAAK-256-automapper-flattening.md`
