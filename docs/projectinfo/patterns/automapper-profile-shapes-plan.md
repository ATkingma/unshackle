---
kingmadoc: 1
feature: "automapper-profile-shapes"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberMapFromPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMaps.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/ProfileShapesTests.cs
---

# Feature: automapper.profile-shapes: profiles nested in a DTO and expression-bodied profile constructors are rewritten; generic, partial and multi-constructor profiles get a precise Manual reason

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-248.

## One-sentence summary

automapper.profile-shapes: the profile rewrite (shared by `CreateMap`, `ForMemberIgnore` and `ForMemberMapFrom`) now accepts a profile nested in a DTO (`private class Mapping : Profile`, Clean Architecture style) and a constructor `public Mapping() => CreateMap<A, B>();`, writing the extension class next to the outermost type, while the shapes it cannot rewrite safely each get their own Manual reason.

## Scope (in / out)

**In scope**

- A profile nested in one or more classes, records or structs (any accessibility of the profile itself); the extension class `<Outer…><Profile>Extensions` goes right after the outermost type, in its namespace.
- A profile constructor with an expression body that is one `CreateMap` chain (with or without `: base(...)`); it becomes an empty block like a block-bodied one.
- `Map<B>(a)` and `Map(a, b)` calls (same file, other file, other project) and nested members of other profiles find the generated methods of such profiles.

**Out of scope (Manual)**

- A generic profile or one nested in a generic type; a partial profile; a profile with several constructors; an outermost type that holds several profiles with maps.
- A nested profile whose maps use a private or protected type (a private DTO nested in a query class), or whose `MapFrom` uses a member that is private/protected or that a simple name only reaches inside the containing type.

## Design

`Find` reports a nested profile at its outermost type (`Anchor`): an extension class must be top-level, and the Fixer can only replace the node a pattern found, so the rewrite replaces the outermost type by [the type with the profile constructor emptied, the extension class]. `Rewrite` derives the anchor from either node (the outermost type from `Find`, or the profile itself from `RewriteMethodsAsync`) and requires exactly one profile with maps in it. Types written in a nested profile are bound again at the anchor (`AtAnchor`, speculative binding) and written with `ToMinimalDisplayString` when they mean something else there (`Command` → `AddNote.Command`); `MemberValues` gets the anchor as position. `OutsideProblem` checks every name in a `MapFrom` lambda: accessible from the whole assembly, and a simple name binds to the same symbol at the anchor. `MapStatements` gives the statements of a block body or the expression of an arrow body, used by `Rewrite` and by `MapCallPattern.GeneratedAsync` (which pairs the i-th method with the i-th statement). `ExtensionsName` (outer type names + profile name) is shared with `NestedMaps.AddRewritten`, which finds profiles rewritten earlier.

## Requirements

- **REQ-1**: WHEN a profile with plain maps is nested in a class THE SYSTEM SHALL empty its constructor and write `public static class <Outer><Profile>Extensions` after the outermost type (proofs `CreateMap/nested-profile`, unit test `Find_NestedProfile_ReportsTheOutermostType`).
- **REQ-2**: WHEN the profile constructor has an expression body (also after `: base(...)`, also on the next line) THE SYSTEM SHALL rewrite it like a block body and leave `public P() { }` with the file's line endings (proofs `CreateMap/expression-bodied`, `CreateMap/expression-bodied-base`, `CreateMap/member-list-nested-profile`, `CreateMap/nested-record`, unit test `Rewrite_ExpressionBodiedConstructorWithCrLf_KeepsTheLineEndings`).
- **REQ-3**: WHEN the outer type is a record or the profile is nested two levels deep THE SYSTEM SHALL rewrite it, writing a type the profile names without its containing types in full (`AddNote.Command`) (proofs `CreateMap/nested-record`, `CreateMap/nested-in-nested`).
- **REQ-4**: WHEN a nested profile uses `ForMember(... Ignore())` or `MapFrom` THE SYSTEM SHALL rewrite it through the same rewrite (proofs `ForMemberIgnore/nested-profile`, `ForMemberMapFrom/nested-profile`).
- **REQ-5**: WHEN a `Map<B>(a)` or `Map(a, b)` call uses a map of a nested profile THE SYSTEM SHALL call its `ToB`/`MapInto`, also from another file (proof `_Combined/nested-profile-map-call`, unit test `FixAsync_NestedProfileInOtherFile_MapCallsFindItsMethods`).
- **REQ-6**: WHEN a member's type pair is mapped by another nested profile (in this run or an earlier one) THE SYSTEM SHALL call that profile's method (proof `_Combined/nested-profiles-nested-map`, unit test `Rewrite_NestedMapOfNestedProfileRewrittenEarlier_CallsItsMethod`).
- **REQ-7**: IF a mapped type is private or protected (a private DTO nested in a class) THEN THE SYSTEM SHALL report Manual (proof `CreateMap/nested-private-dto`).
- **REQ-8**: IF a `MapFrom` expression uses a private/protected member, or a simple name that means something else next to the outermost type THEN THE SYSTEM SHALL report Manual (proofs `ForMemberMapFrom/nested-private-member`, `ForMemberMapFrom/nested-outer-member`).
- **REQ-9**: IF the outermost type holds several profiles with maps, or `<Outer><Profile>Extensions` already exists THEN THE SYSTEM SHALL report Manual (proofs `CreateMap/nested-several-profiles`, `CreateMap/nested-name-taken`).
- **REQ-10**: IF the profile is generic, partial or has several constructors THEN THE SYSTEM SHALL report Manual with that specific reason (proofs `CreateMap/generic`, `CreateMap/partial`, `CreateMap/several-constructors`).

## Assumptions

- A nested profile behaves in AutoMapper exactly like a top-level one: `AddAutoMapper` and assembly scanning find nested `Profile` subclasses (private ones too) and run their parameterless constructor; nesting only changes C# name lookup, which `AtAnchor` and `OutsideProblem` cover.
- An expression-bodied constructor runs the same `CreateMap` chain as a block with that one statement.

## Risks

- A finding of a nested profile is reported at the line of the outermost type (the node the rewrite replaces), not at the profile.
- An outermost type with two profiles (cats `AddPRI`, 1 type) stays Manual, although each profile on its own would be rewritable.

## Models

Class diagram, sequence and decision tree: [automapper-profile-shapes-modellen.md](automapper-profile-shapes-modellen.md).

## Open questions

- None.
