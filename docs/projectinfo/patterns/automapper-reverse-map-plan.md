---
kingmadoc: 1
feature: "automapper-reverse-map"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMaps.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberMapFrom/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/CreateMapPatternTests.cs
    - src/Unshackle.Tests/Core/MapCallPatternTests.cs
---

# Feature: automapper.reverse-map: CreateMap<A, B>().ReverseMap() also gets A ToA(this B source)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-249.

## One-sentence summary

automapper.reverse-map: for `CreateMap<A, B>().ReverseMap()` the profile rewrite writes, next to `B ToB(this A source)`, an `A ToA(this B source)` that does exactly what AutoMapper 14's reverse map does (measured), and reports Manual where AutoMapper unflattens or does something unclear.

## Scope (in / out)

**In scope**

- `.ReverseMap()` at the end of a `CreateMap<A, B>(...)` chain in a profile constructor, with or without a constant `MemberList`, forward `ForMember(... Ignore())` / `ForMember(... MapFrom(...))`, and `ForMember(... Ignore())` / `ForMember(... MapFrom(...))` after `ReverseMap()` (they configure the reverse map).
- Nested members and lists through other maps, also reverse maps (`NestedMaps` registers `B → A`).
- `mapper.Map<A>(b)` → `b.ToA()` and `mapper.Map(b, a)` → `b.MapInto(a)` on the reverse map (`MapCallPattern.GeneratedAsync`, `MapIntoPattern`).

**Out of scope (Manual)**

- Unflattening: a forward `MapFrom(s => s.X.Y)` (a path of two or more members), a forward `MapFrom(s => s)`, and a destination member of the forward map AutoMapper could have flattened (a name that starts with a member name of `A`, not `Ignore()`d).
- A forward `MapFrom` with a cast, and two forward `MapFrom`s that read the same member.
- A reverse destination member without a same-named source member that AutoMapper may still fill (another case, `GetX` member or method, a flattening prefix).
- A reversed `MapFrom` into a member without a public setter.
- Other options after `ReverseMap()` (`ForPath`, `ConstructUsing`, ...) and a profile that also has its own `CreateMap<B, A>`.
- Conversions, constructors and the rest exactly as for a forward map (own queue rows).

## Design

`ReverseMap` is a capability of the shared profile rewrite in `CreateMapPattern` (used by `ForMemberIgnorePattern` and `ForMemberMapFromPattern`), like nested maps and the member list: no pattern class of its own.

- `TryParseMap` finds the `ReverseMap()` call of AutoMapper in the chain (`ReverseMapCall`); the `ForMember` options outside it go to the reverse map, those inside to the forward map (`TryParseOptions`).
- `Rewrite` computes the forward map as before, then `Reverse` for `B → A`: it reverses one-member `MapFrom` paths (`MemberPath`, `Unwrap`), checks for unflattening, and calls `Problem(B, A, reverseOptions, ..., reversed)`. With `reversed` set, `Problem` assigns `X = source.Y` for a reversed `MapFrom`, after the reverse options and before same-named members, and leaves a destination member without any source alone (`MayFlatten` false) because the reverse map has `MemberList.None`.
- The reverse method comes right after its forward method; `MapCallPattern.GeneratedAsync` pairs methods and maps in the same order (`HasReverseMap`). `NestedMaps.AddProfiles` registers the reverse pair too. Two maps for one type pair in a profile are Manual (the methods would collide).

## Requirements

- **REQ-1**: WHEN a profile holds `CreateMap<A, B>().ReverseMap()` THE SYSTEM SHALL write `A ToA(this B source)` with the same-named members of `A` right after `ToB` (proofs `CreateMap/reverse-map`, `CreateMap/member-list-reverse-map`).
- **REQ-2**: WHEN a member of `A` has no member in `B` that AutoMapper could map from THE SYSTEM SHALL leave it out of `ToA` (proofs `CreateMap/reverse-map` `CreationDate`, `CreateMap/member-list-reverse-map` `PasswordHash`, `_Combined/reverse-map-call` `CreatedBy`).
- **REQ-3**: WHEN the forward map has `ForMember(... Ignore())` THE SYSTEM SHALL still map that member in reverse (proofs `_Combined/reverse-map-call`, `ForMemberIgnore/reverse-map-options`, `ForMemberIgnore/reverse-map`; unit test case "Rank").
- **REQ-4**: WHEN `ReverseMap()` is followed by `ForMember(... Ignore())` or `ForMember(... MapFrom(...))` THE SYSTEM SHALL apply them to `ToA` only (proofs `ForMemberIgnore/reverse-map-options`, `ForMemberMapFrom/reverse-map`), also over a reversed `MapFrom` (unit test `Rewrite_ReverseOptionAndReversedMapFrom_OptionWins`).
- **REQ-5**: WHEN the forward map has `ForMember(d => d.Y, o => o.MapFrom(s => s.X))` THE SYSTEM SHALL write `X = source.Y` in `ToA` (proof `ForMemberMapFrom/reverse-map`).
- **REQ-6**: WHEN a member of the reverse map has its own map (also a reverse one) or is a list of such THE SYSTEM SHALL call it as for a forward map (proof `CreateMap/reverse-map-nested`).
- **REQ-7**: WHEN `mapper.Map<A>(b)` or `mapper.Map(b, a)` uses the reverse map THE SYSTEM SHALL rewrite it to `b.ToA()` / `b.MapInto(a)` (proof `_Combined/reverse-map-call`, unit test `FixAsync_ReverseMapInOtherFile_CallsReverseMethod`).
- **REQ-8**: IF AutoMapper unflattens in reverse (forward `MapFrom` path of two or more members or of the whole source, a flattened forward member) THEN THE SYSTEM SHALL report Manual (proof `ForMemberMapFrom/reverse-map-unflattening`; unit test cases "maps the whole A", "may be unflattened").
- **REQ-9**: IF a reverse member may be filled another way (case, `GetX`, flattening prefix), a reversed `MapFrom` casts, reads a member twice or targets a member without a setter, or a reverse `MapFrom` dereferences null in a way that is not a plain member chain THEN THE SYSTEM SHALL report Manual (a plain chain is null-safe since TAAK-263, proof `ForMemberMapFrom/reverse-map-may-be-null`; proof `CreateMap/reverse-map-get-method`; unit test `Rewrite_ReverseMapAutoMapperDoesMoreFor_ReportsManual`).
- **REQ-10**: IF `ReverseMap()` is followed by another option, or the profile also has its own `CreateMap<B, A>` THEN THE SYSTEM SHALL report Manual (proofs `CreateMap/reverse-map-options`, `CreateMap/reverse-map-duplicate`).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-249 **Evidence**, 30+ cases). Rewrites only cover what was measured; the rest is Manual.
- The forward map must be rewritable as well: a profile is rewritten all-or-nothing.

## Risks

- `MayFlatten` and the unflattening check are conservative name tests (prefix, `Get`, case); they can give Manual where AutoMapper would leave a member alone, never the other way round.

## Models

Class diagram, sequence and decision tree: [automapper-reverse-map-modellen.md](automapper-reverse-map-modellen.md).

## Open questions

- None.
