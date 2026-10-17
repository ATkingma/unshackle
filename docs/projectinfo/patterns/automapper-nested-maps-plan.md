---
kingmadoc: 1
feature: "automapper-nested-maps"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberValues.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMaps.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMap.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.nested-maps: a member whose type pair has its own CreateMap → X = source.X?.ToY(), lists and arrays copied element by element

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-245.

## One-sentence summary

automapper.nested-maps: in the `ToB` method that the profile rewrite writes, a member AutoMapper maps through another map (`Address` → `AddressDto`, `List<OrderLine>` → `List<OrderLineDto>`) or copies (`List<string>` → `List<string>`) gets the same value by hand, with AutoMapper 14's measured null behaviour.

## Scope (in / out)

**In scope**

- A destination member (same-named source member or `MapFrom` expression) whose type pair `A` → `B` has a `CreateMap<A, B>` in a profile one of the profile patterns rewrites: in the same profile, another profile of the project, or a profile in a project it depends on.
- Single members: class → class, struct → anything, `A?` → `B?` (structs), class → struct; also the same type when `CreateMap<T, T>` exists.
- Collections: source and destination each `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>` or `IReadOnlyList<T>`; elements through such a map or of the same type.

**Out of scope (Manual)**

- A pair without its own map, a pair mapped by several profiles, a map whose extension class becomes internal in another project, a source type that already has a member `ToB`.
- Cycles (the nested destination type can reach the outer destination type again).
- Dictionaries, sets, `Collection<T>` and own collection types; other nullability mixes of structs (`A?` → `B`).
- A map with a nested or copied member that also needs `MapInto` (a `Map(source, destination)` call).
- `Map<List<B>>(list)` calls (row `automapper.map-call`), open generic maps.

## Design

Nested maps are part of the shared profile rewrite of `CreateMapPattern` (also used by `ForMemberIgnorePattern` and `ForMemberMapFromPattern`), not a pattern class of its own: every profile is claimed by exactly one profile pattern, and a nested member can occur in any of them. `NestedMaps` indexes every `CreateMap<A, B>` of the profiles by type pair (fully qualified names, so pairs from other compilations match), with the method name the rewrite will give it (`CreateMapPattern.MethodName`), its namespace and whether its class becomes internal. Without the solution it holds the project's own profiles; `RewriteAsync` adds the projects the profile's project depends on. A profile in the same file that an earlier profile pattern already rewrote is found through its `<Profile>Extensions` class. Because the fix is all-or-nothing, a map found there is either written or nothing is applied.

`MemberValues.Problem` turns a source value and the two types into the assigned expression (or a Manual reason), collects the usings it needs (`System.Linq`, the other profile's namespace) and reports whether the member is copied. `MapCallPattern.GeneratedAsync` now rewrites the profiles with the solution, so a `Map` call also finds a profile whose nested map lives in another project.

## Requirements

- **REQ-1**: WHEN a destination member's type pair has its own CreateMap in the same profile THE SYSTEM SHALL assign `source.X?.ToY()` (class), `source.X.ToY()` (struct source), `source.X?.ToY()` (`A?` → `B?`) or `source.X?.ToY() ?? default` (class → struct) (proof `CreateMap/nested`).
- **REQ-2**: WHEN the member is a list, array or one of the supported interfaces THE SYSTEM SHALL copy it with `?.Select(x => x?.ToY())` (`x.ToY()` for struct elements) and `ToList()`/`ToArray()` and `?? []` for a null source, adding `using System.Linq` (proofs `CreateMap/nested-collections`, `CreateMap/collection`).
- **REQ-3**: WHEN the element type is the same and has no map THE SYSTEM SHALL copy the collection into a new one (`source.Xs?.ToList() ?? []`) (proofs `CreateMap/collection`, `ForMemberMapFrom/collection`, `CreateMap/nested-collections`).
- **REQ-4**: WHEN a `CreateMap<T, T>` exists for a member of the same type T THE SYSTEM SHALL call that map (proof `CreateMap/nested-same-type`).
- **REQ-5**: WHEN the map lives in another profile THE SYSTEM SHALL call its method and add a using for its namespace when needed, also in another file or a project depended on (proofs `CreateMap/nested-other-profile`, `_Combined/nested-profile-rewritten-earlier`, unit tests `Rewrite_NestedMapInOtherFileOfTheProject_IsFoundWithoutTheSolution`, `FixAsync_NestedMapInProjectDependedOn_CallsItsMethod`).
- **REQ-6**: WHEN the generated code is in a nullable-enabled context and the destination is non-nullable THE SYSTEM SHALL add `!` to the nested call (proof `CreateMap/nested-nullable`).
- **REQ-7**: WHEN a `MapFrom` expression has a type pair with its own map or is a supported collection THE SYSTEM SHALL apply REQ-1 to REQ-3 to the expression (proofs `ForMemberMapFrom/nested`, `ForMemberMapFrom/collection`).
- **REQ-8**: IF the pair has no map, several maps, an internal map in another project, or forms a cycle THEN THE SYSTEM SHALL report Manual (proofs `CreateMap/nested-no-map`, `CreateMap/nested-several`, `CreateMap/nested-cycle`, unit test `FixAsync_NestedMapInternalInOtherProject_ReportsManual`).
- **REQ-9**: IF the collection is of another kind (dictionary, set, own type) THEN THE SYSTEM SHALL report Manual (proof `CreateMap/collection-dictionary`); below C# 12 the empty collection is `new List<B>()` / `Array.Empty<B>()` (unit test `Rewrite_BeforeCSharp12_FallsBackToNewEmptyCollections`).
- **REQ-10**: IF a map with a nested or copied member needs `MapInto` THEN THE SYSTEM SHALL report Manual (unit test `FixAsync_MapIntoWithNestedMember_ReportsManual`); a `Map<B>(a)` call on such a map becomes `a.ToB()` (proof `_Combined/nested-map-call`).
- **REQ-11** (TAAK-273): WHEN the destination type of a nested pair reaches the outer destination only through members that have no source counterpart (a destination-only type cycle such as `BoxModel.Order`) THE SYSTEM SHALL call the nested method; IF the cycle runs through members with a source counterpart (also through flattening, `Get` methods or a member type it cannot pair) THEN THE SYSTEM SHALL report Manual. Measured (AutoMapper 14.0.0, probe TAAK-273): AutoMapper recurses only through mapped members and does not preserve references for a destination-only cycle (a shared source object gives two copies, like the generated method); for a real cycle it preserves references, also for tree data with a shared node (proofs `_Combined/cycle-destination-only`, `CreateMap/cycle-source`).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-245 **Evidence**): a null nested object maps to null, also over a destination initializer; a null collection maps to an empty `List<B>`/`B[]`; null elements stay null; interface destinations get a `List<B>`; same-type collections become new collections with the same element references; class → struct with a null source gives `default`; `Map(src, dest)` maps into the existing nested object and list.
- The configuration options that change this (`AllowNullCollections`, `AllowNullDestinationValues`) live outside a profile's `CreateMap` statements; a profile that sets them is not a plain CreateMap profile and stays Manual.

## Risks

- The cycle check is conservative: a self-referencing DTO type (e.g. a tree) is Manual even when the member that closes the cycle is ignored.
- The `!` in a nullable context hides that the destination can be null; it is what AutoMapper does too.

## Models

Class diagram, sequence and decision tree: [automapper-nested-maps-modellen.md](automapper-nested-maps-modellen.md).

## Open questions

- None.
