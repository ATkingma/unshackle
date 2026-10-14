---
kingmadoc: 1
feature: "automapper-create-map"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
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
