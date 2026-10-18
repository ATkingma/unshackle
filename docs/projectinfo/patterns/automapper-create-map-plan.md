---
kingmadoc: 1
feature: "automapper-create-map"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-14]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
---

# Feature: automapper.create-map: Profile with CreateMap<A,B>() without options → ToB extension method

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-146.

## One-sentence summary

automapper.create-map: a `Profile` whose constructor only contains `CreateMap<A, B>()` without options gets a static class `<Profile>Extensions` with per map `static B ToB(this A source) => new() { X = source.X, ... }`; the constructor becomes empty.

## Scope (in / out)

**In scope**

- A top-level, non-generic, non-partial `Profile` subclass with one constructor that only has `CreateMap<A, B>();` statements.
- Destination members that AutoMapper writes: public properties with a public `set`/`init` and public non-readonly fields, also inherited.
- AutoMapper's `Map`/`ProjectTo` calls: Manual, so the whole fix (AllOrNothing) changes nothing as long as AutoMapper still maps.

**Out of scope**

- `ForMember`, `ReverseMap`, `ConvertUsing` and other options (rows 2, 3 and 7 of `claude/tasks/automapper-queue.md`): Manual.
- Removing the empty `Profile` and the AutoMapper package: only once `map-call` and `add-automapper` exist.

## Requirements

- **REQ-1**: WHEN a profile only has `CreateMap<A, B>()` without options THE SYSTEM SHALL write per map a `ToB(this A source)` with all same-named members of the same type and empty the constructor (proof `simple`, also a struct destination and fields).
- **REQ-2**: WHEN A or B is `internal` THE SYSTEM SHALL make the class and methods `internal` (proof `internal-types`).
- **REQ-3**: IF a statement is not a `CreateMap<A, B>()` without options THEN THE SYSTEM SHALL report Manual for the whole profile (proof `options`).
- **REQ-4**: IF a destination member has no source member with exactly the same name (flattening, `GetX()`, different case) THEN THE SYSTEM SHALL report Manual (proof `flattening`, unit test for case).
- **REQ-5**: IF the types differ or the member is a collection THEN THE SYSTEM SHALL report Manual (proofs `conversion`, `collection`). _Superseded in part by TAAK-245_: a member whose type pair has its own CreateMap and a list or array are rewritten now ([automapper-nested-maps-plan.md](automapper-nested-maps-plan.md)); `collection` became an `.after.cs`. _Superseded in part by TAAK-247_: AutoMapper's built-in conversions are rewritten where a hand-written form gives the same result ([automapper-conversions-plan.md](automapper-conversions-plan.md)); `conversion` became an `.after.cs`, `conversion-operator` keeps the Manual reason.
- **REQ-6**: IF B has no public parameterless constructor or is abstract THEN THE SYSTEM SHALL report Manual (proof `constructor`).
- **REQ-7**: WHEN AutoMapper's `Map`/`ProjectTo` is called THE SYSTEM SHALL report Manual, so the fix changes nothing (proof `map-call`; since TAAK-157 only `ProjectTo`, `Map(src, dest)` and `Map` with options, proof `map-into`).
- **REQ-8** (TAAK-261): WHEN a writable destination member has no source member AutoMapper reads (same name in any case, `GetX`, flattening), or is get-only without a setter and not a collection, THE SYSTEM SHALL leave it out of the object initializer, so the new object keeps its initializer or default exactly as AutoMapper 14 does (proofs `CreateMap/unmatched-member`, `ForMemberIgnore/unmatched`). IF the solution (without it: the compilation) calls `AssertConfigurationIsValid` anywhere THEN an unmatched member is Manual (proof `unmatched-asserted`): that check throws on such a member, so passing tests mean AutoMapper maps it in a way the fix does not see. IF the source has the member and the destination has a non-public setter (AutoMapper writes it) or is a get-only collection (AutoMapper clears it and adds the source items) THEN Manual (proofs `private-setter`, `get-only-collection`).
- **REQ-9** (TAAK-273): IF AutoMapper writes a destination member through its non-public setter THEN THE SYSTEM SHALL report Manual with a reason that says this is principally manual (generated code has no access to the setter). The constructor route of TAAK-271 (a public constructor that only assigns members from its parameters) is not built: no corpus case qualifies (fiapgames' constructor also sets `Ativo`, baseapi has none).
- **REQ-10** (TAAK-273): WHEN a member conversion has no map and AutoMapper has no built-in conversion either (a class or struct without `IConvertible`, `[TypeConverter]` or a conversion operator, into a number, `bool` or `char`) THE SYSTEM SHALL add to the Manual reason that AutoMapper throws there too, so the source mapping is broken and principally manual (restapi `List<DayEntity>` → `List<int>`, measured in TAAK-271; proof `CreateMap/broken-conversion`).
- **REQ-11** (TAAK-279): WHEN the plan shares a pair (a class under test keeps calling `IMapper` for it) THE SYSTEM SHALL generate its `ToB()` and leave its `CreateMap` statement in the profile constructor ([automapper-mocked-imapper](../features/automapper-mocked-imapper-plan.md)).
- **REQ-12** (TAAK-282): WHEN a destination member is `required` and nothing maps it (Ignore, no source, get-only source) THE SYSTEM SHALL name it in the object initializer as `Member = default` (`default!` in a nullable-warning context for a non-nullable reference type), because AutoMapper 14.0.0 leaves it at `default` without an error (probe against 14.0.0: unmapped required `int`/`string` stay `0`/`null`, one with an initializer keeps it, one set in the constructor keeps it) but the compiler rejects an initializer that omits it (CS9035). IF the member has an initializer or the type declares a constructor (AutoMapper keeps what they gave it, `= default` would overwrite it) or the type comes from metadata THEN Manual (unit tests `Rewrite_RequiredMember*`, proofs `_Combined/required-members`, `_Combined/project-to-required`).
- **REQ-13** (TAAK-280): WHEN the removed `CreateMap` statements carried half of a `#region`/`#endregion` pair THE SYSTEM SHALL drop the other half (`ProfileDirectives.Repair`), so the profile still compiles (CS1028). IF a removed map sat in `#if`/`#else`/`#endif` THEN Manual: the mapping method would exist in every build configuration (proof `_Combined/region-directives`, unit test `Rewrite_DirectivesAroundCreateMap_StayBalancedOrManual`).
- **REQ-14** (TAAK-286): WHEN a class outside a Profile builds one `new MapperConfiguration(cfg => ...)` whose lambda only calls `cfg.CreateMap<A, B>()` (`CreateProjection`) THE SYSTEM SHALL treat that lambda like a Profile constructor: the same rewrite, the extension class `<Class>Extensions` next to the class, and every consumer (Map calls, ProjectTo, nested maps, kept maps) reads its maps through `CreateMapPattern.MapStatements`. IF the lambda also uses `cfg` otherwise (`AddProfile`, a setting), the configuration is not the initializer of a field or property (a local variable, direct use such as `new MapperConfiguration(...).AssertConfigurationIsValid()`), or the class builds several such configurations THEN it is not a map class: Manual as before (proof `_Combined/configuration-maps-map-call`, unit tests `ConfigurationMapsTests`). Measured on hospitalappt: the remaining Manual is `ReverseMap` with unflattening or a source member without a destination counterpart, not the shape of the configuration (follow-up in TAAK-286).

## Assumptions

- AutoMapper 14.0.0 assigns equal, non-collection types directly (no copy); collections it copies to a new instance.
- AutoMapper ignores a get-only destination member without a source member; with a source member it can fill it (existing collection), hence Manual.

## Risks

- The Fixer does find `Map` calls in **another** project than the profile's (it runs all projects), but not outside the solution; there a missing type map would only fail at runtime.
- `ToB` gives a `NullReferenceException` on `null`, where `mapper.Map` gave `null`: relevant for row 4 (`map-call`).

## Models

Class diagram, sequence and decision tree: [automapper-create-map-modellen.md](automapper-create-map-modellen.md).

## Open questions

- None.
