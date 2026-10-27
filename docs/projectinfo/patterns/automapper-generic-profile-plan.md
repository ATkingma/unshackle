---
kingmadoc: 1
feature: "automapper-generic-profile"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericProfilePattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericProfileCode.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Core/Generics/TypeArgumentClosure.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/GenericProfile/
    - src/Unshackle.Tests/Core/AutoMapper/GenericProfilePatternTests.cs
---

# Feature: automapper.generic-profile: a generic Profile closed per type argument

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-27 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-334 and council round 15, decisions A13 and A14 (`claude/tasks/COUNCIL-round-15.md`).

## One-sentence summary

`class CopyProfile<T> : Profile { CopyProfile() { CreateMap<T, T>(); } }` gets one `B ToB(this A source)` per closed construction
(`class RoleProfile : CopyProfile<Role>`, `new CopyProfile<Role>()`, `AddProfile<CopyProfile<Role>>()`) when every closed pair can be written;
otherwise Manual `GenericProfileOpenSet`, naming the open use or the pair.

## Scope (in / out)

**In scope**

- A top-level generic class deriving from AutoMapper's `Profile`, with one constructor whose statements are all `CreateMap<A, B>();`
  without options, where `A` and `B` are a type parameter of the profile or a plain type.
- Closed constructions found by `TypeArgumentClosure` over the class type parameters (`CloseByUses`: closed subclasses, constructions,
  written type arguments such as `AddProfile<P<X>>`), followed through generic code up to depth 3, constraints filtering.
- Each closed pair is checked and written like a `CreateMap<A, B>()` without options (`CreateMapProblems.Problem`, `MapMethodWriter.Extensions`).

**Out of scope (Manual `GenericProfileOpenSet`)**

- An open use: `typeof(P<>)` (open registration), `MakeGenericType`, a public profile in a packable project (R1, with the A13 advice),
  `InternalsVisibleTo` to an assembly outside the solution (R2), no closed construction at all, a chain deeper than 3.
- A statement other than a plain `CreateMap<A, B>();` (options, `ReverseMap`, `ForMember`, settings), a type argument inside another generic
  type (`List<T>`), a pair used by `Map(source, destination)` or `ProjectTo`, a pair that `CreateMapProblems` rejects.
- A partial profile, several constructors, a profile nested in a generic type (stays with `CreateMapPattern`, Manual "is generic").
- Routing `mapper.Map<X>(x)` calls on a closed pair to the generated method: not done yet; such a call stays Manual in `map-call`,
  and the fix is all-or-nothing (see Risks).

## Design

`GenericProfilePattern` (`ISolutionPattern`, registered after `GenericMapPattern`) finds top-level generic profiles (`IsGenericProfile`).
`CreateMapPattern` (and its ForMember/ValueResolver subclasses) and the partial plan `MapOwners` skip them, so exactly one pattern
reports them. `TypeArgumentClosure.Close(profileType.TypeParameters)` already handles type parameters of a class (`CloseByUses`, built for
A11); A14 reuses it unchanged, including R1/R2 and the reflection checks. For each closed set every `CreateMap` is substituted, mapped
into the profile's compilation, checked by `CreateMapProblems.Problem` and collected. Only when every pair passes is the constructor
emptied and `<Profile>Extensions` written next to the profile; the generic profile itself stays (empty), so its subclasses keep compiling.

A13 lives in `TypeArgumentClosure.ReachableOutside`: the R1 reason ends with
"Set `<IsPackable>false</IsPackable>` in <project file> if this library is not published", shared by `generic-map` and `generic-profile`.

## Requirements

- **REQ-1**: WHEN every closed construction of a generic profile is known THE SYSTEM SHALL write one mapping method per closed pair and empty the constructor (proof `closed-subclasses`).
- **REQ-2**: WHEN the profile is closed by `new P<X>()` or `AddProfile<P<X>>()` THE SYSTEM SHALL count it as a closed construction (`FixAsync_ClosedConstruction_WritesMethodPerPair`).
- **REQ-3**: IF `typeof(P<>)` is used THEN Manual `GenericProfileOpenSet` naming the place (proof `open-registration`).
- **REQ-4**: IF the profile is public in a packable project THEN Manual with the A13 advice (proofs `packable-library`, `skoruba-shape`; `FixAsync_PublicProfile_FollowsIsPackable`); with `IsPackable=false` it is closed.
- **REQ-5**: IF a map statement has options or a type argument inside another generic type THEN Manual naming it (`FixAsync_UnsupportedMap_IsOpenSet`).
- **REQ-6**: IF no construction closes the profile THEN Manual (`FixAsync_NoClosedConstruction_IsOpenSet`).
- **REQ-7**: WHEN the R1 reason is reported for a generic `Map<T>` THE SYSTEM SHALL name the A13 fix too (proof `GenericMap/packable-public-base`, `FixAsync_PublicMethodInLibraryWithProjectProperties_FollowsIsPackable`).

## Assumptions

- AutoMapper's assembly scanning skips open generic profile definitions; it only instantiates closed subclasses, which are written uses.
- A pair that two closed sets produce twice is written once.

## Risks

- skoruba's generic profiles are public in packable libraries (A13): its corpus delta is expected to stay low until `IsPackable=false` is set.
- A `Map` call on a closed pair keeps the whole fix Manual (all-or-nothing) until `MapCallPattern.GeneratedAsync`/`NestedMaps` index the
  generated methods of generic profiles (council question in TAAK-334).

## Models

Classes, sequence and decision tree: [automapper-generic-profile-modellen.md](automapper-generic-profile-modellen.md).

## See also

- [automapper-generic-profile-verify.md](automapper-generic-profile-verify.md)
- [automapper-generic-map-plan.md](automapper-generic-map-plan.md)
- [automapper-profile-shapes-plan.md](automapper-profile-shapes-plan.md)
- `claude/tasks/COUNCIL-round-15.md` (A13, A14)
