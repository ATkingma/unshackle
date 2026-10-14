---
kingmadoc: 1
feature: "automapper-for-member-ignore"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/
---

# Feature: automapper.for-member-ignore: ForMember(d => d.X, o => o.Ignore()) → leave the member out of the ToB method

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 by KingmaDoc |

> Drawn up from TAAK-155.

## One-sentence summary

automapper.for-member-ignore: a `CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore())` in a `Profile` becomes, just like `automapper.create-map`, a `ToB(this A source)` extension method in which the ignored member X is not assigned.

## Scope (in / out)

**In scope**

- One or more `.ForMember(d => d.X, o => o.Ignore())` directly on `CreateMap<A, B>()` (lambdas with one parameter, `X` a member of B).
- An ignored member that does have a source member: also left out (AutoMapper would not fill it).
- An ignored member without a source member: previously Manual (no member with the same name), now skipped.

**Out of scope**

- `MapFrom` (since TAAK-156 `automapper.for-member-map-from`) and other `ForMember` options (row 7): Manual or own pattern.
- `ForMember("X", ...)` with a string name, nested members (`d => d.A.B`), block lambdas, `ReverseMap`: Manual.
- An ignored member that B does not have as public: Manual.

## Design

`ForMemberIgnorePattern` inherits from `CreateMapPattern` and shares `Rewrite`: a profile is replaced as a whole by `[profile, <Profile>Extensions]`, so two separate patterns on the same class would overwrite each other. `ForMemberIgnorePattern.Find` claims the profiles with a `ForMember(..., o => o.Ignore())`, `CreateMapPattern.Find` the rest. The Map/ProjectTo calls (AllOrNothing) stay with `CreateMapPattern`.

## Requirements

- **REQ-1**: WHEN a `CreateMap<A, B>()` has one or more `ForMember(d => d.X, o => o.Ignore())` THE SYSTEM SHALL write the ToB method without X, also when A has a member X (proofs `simple`, `mixed`, unit test `Rewrite_IgnoredMemberHasSourceMember_LeavesItOut`).
- **REQ-2**: WHEN an ignored member has no source member THE SYSTEM SHALL not report it as a missing member (proofs `simple`, `mixed`).
- **REQ-3**: IF a member is not ignored and has no source member with the same name and type THEN THE SYSTEM SHALL report Manual, as with `create-map` (proof `unmatched`).
- **REQ-4**: IF a `ForMember` has an option other than `Ignore()` (e.g. `MapFrom`), uses a string name or is not directly on `CreateMap` (e.g. after `ReverseMap`) THEN THE SYSTEM SHALL report Manual for the whole profile (proofs `string-name`, `reverse-map`; MapFrom is its own pattern since TAAK-156).
- **REQ-5**: IF the ignored member does not exist as a public member of B THEN THE SYSTEM SHALL report Manual (unit test `Rewrite_IgnoredMemberNotPublic_ReportsManual`).
- **REQ-6**: WHEN a profile occurs THE SYSTEM SHALL have it found by exactly one of `CreateMapPattern` and `ForMemberIgnorePattern` (unit test `Find_ProfileWithOrWithoutIgnore_IsClaimedByOneOfThePatterns`).

## Assumptions

- `Ignore()` leaves the destination member at the default value of `new B()`; the new code does the same.
- A `ForMember` with `Ignore` on a get-only member changes nothing: the member is not assigned anyway.

## Risks

- `ForMember` is checked against the AutoMapper assembly; an own extension method `ForMember` with the same lambda shape is not rewritten (symbol check).

## Models

Class diagram, sequence and decision tree: [automapper-for-member-ignore-modellen.md](automapper-for-member-ignore-modellen.md).

## Open questions

- None.
