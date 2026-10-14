---
kingmadoc: 1
feature: "automapper-member-list"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/CreateMapPatternTests.cs
---

# Feature: automapper.member-list: CreateMap<A, B>(MemberList.X) is migrated like CreateMap<A, B>(), the argument is dropped

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-246.

## One-sentence summary

automapper.member-list: the `MemberList` argument of `CreateMap<A, B>(MemberList.Destination | Source | None)` only tells `AssertConfigurationIsValid` which members must be mapped, so the profile rewrite drops it and writes the same `B ToB(this A source)` as for `CreateMap<A, B>()`.

## Scope (in / out)

**In scope**

- `CreateMap<A, B>(MemberList.X)` and `CreateMap<A, B>(memberList: MemberList.X)` in a profile constructor, any constant value of AutoMapper's `MemberList` (also `AutoMapper.MemberList.X`), with or without `.ForMember(... Ignore())` / `.ForMember(... MapFrom(...))`.
- Everything the profile rewrite already does with such a map: nested members, `MapInto`, `Map<B>(a)` calls (`MapCallPattern`).

**Out of scope (Manual, unchanged)**

- A non-constant `MemberList` argument (parameter, field, method call).
- `.ReverseMap()`, `.MaxDepth()`, `.ConstructUsing()`, `.ForMember(... Condition())` and other options next to the member list (own queue rows).
- Nested profiles and profiles whose constructor is expression-bodied (most cats profiles), and `cfg.CreateMap` in a `MapperConfiguration` (row 18).
- Destination members without a same-named source member: still Manual, also under `MemberList.None`/`Source`.

## Design

The member list is a capability of the shared profile rewrite in `CreateMapPattern` (used by `ForMemberIgnorePattern` and `ForMemberMapFromPattern`), not a pattern class: there is no node that only this capability claims. `TryParseMap` accepted only `CreateMap<A, B>()` with zero arguments; it now also accepts exactly one argument for which `IsMemberList` holds: no `ref`/`out`, no name or the name `memberList`, its type is the enum `MemberList` of the AutoMapper assembly, and `SemanticModel.GetConstantValue` has a value. The argument is not used further: `Problem` computes the members exactly as without it.

## Requirements

- **REQ-1**: WHEN a profile holds `CreateMap<A, B>(MemberList.Destination | Source | None)` THE SYSTEM SHALL write the same `ToB` method as for `CreateMap<A, B>()` (proof `CreateMap/member-list`, unit test `Rewrite_ConstantMemberList_SameAsWithout`).
- **REQ-2**: WHEN the argument is named (`memberList:`), qualified (`AutoMapper.MemberList.X`) or another constant of `MemberList` THE SYSTEM SHALL treat it as in REQ-1 (proof `CreateMap/member-list-named`, unit test `Rewrite_ConstantMemberList_SameAsWithout`).
- **REQ-3**: WHEN the map with a member list carries `ForMember(... Ignore())` or `ForMember(... MapFrom(...))` THE SYSTEM SHALL apply those options as without the member list (proofs `ForMemberIgnore/member-list`, `ForMemberMapFrom/member-list`).
- **REQ-4**: WHEN a `Map<B>(a)` call uses a map with a member list THE SYSTEM SHALL rewrite it to `a.ToB()` (proof `_Combined/member-list-map-call`).
- **REQ-5**: IF the argument is not a constant `MemberList` THEN THE SYSTEM SHALL report Manual (proof `CreateMap/member-list-variable`, unit test `Rewrite_MemberListParameter_ReportsManual`).
- **REQ-6**: IF the map also has `.ReverseMap()` or sits in a nested profile THEN THE SYSTEM SHALL report Manual as before (proofs `CreateMap/member-list-reverse-map`, `CreateMap/member-list-nested-profile`).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-246 **Evidence**): `Map`, `Map(src, dest)` and the reverse map give identical results for all three values; only `AssertConfigurationIsValid` differs. An IL scan shows `TypeMap.ConfiguredMemberList` is only read by the configuration validator, the exception message and the `MappingExpression(TypeMap)` constructor that copies it.
- A profile migrated this way no longer has a configuration to validate; tests that call `AssertConfigurationIsValid` belong to row 18.

## Risks

- None for the mapping. The validation the member list steered disappears with AutoMapper itself; the hand-written method makes every mapped member explicit.

## Models

Class diagram, sequence and decision tree: [automapper-member-list-modellen.md](automapper-member-list-modellen.md).

## Open questions

- None.
