---
kingmadoc: 1
feature: "automapper-map-into"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapIntoPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/MapInto/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.map-into: mapper.Map(a, b) → a.MapInto(b)

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Project**   | Unshackle                                                         |
| **Status**    | Approved                                                          |
| **Generated** | 2026-10-12 by KingmaDoc (manually, `kingmadoc` not available)     |

> Drafted from TAAK-158.

## One-sentence summary

automapper.map-into: `mapper.Map(a, b)` (`Map<A, B>(A, B)`) becomes `a.MapInto(b)`, an extension method that the profile rewrite writes next to `ToB` for every map used by such a call in the solution.

## Scope (in / out)

**In scope**

- `Map(a, b)` and `Map<A, B>(a, b)` from AutoMapper (second parameter of type `B`, no options lambda), on a name or a chain of names.
- A `MapInto(this A, B)` that already exists (in scope or in the project's source code).
- `CreateMap<A, B>` in a profile that the profile patterns rewrite: `CreateMapPattern.RewriteAsync` then adds `B MapInto(this A source, B destination)` with the same assignments as `ToB` (Ignore omitted, MapFrom expressions) and `return destination;`.

**Out of scope (Manual)**

- Collections, source or destination `object`/`dynamic`, source or destination MaybeNull, receiver with side effects.
- No profile or a Manual profile, several profiles, internal `MapInto` in another project, an existing member `MapInto` on `A`.
- `Map` with options and `ProjectTo`: remain Manual in `CreateMapPattern` (row 7).

## Design

`MapIntoPattern` is an `ISolutionPattern` and follows `MapCallPattern`: first an existing `MapInto`, otherwise `MapCallPattern.GeneratedAsync` (same profile search; namespace and internal taken from the generated class). `CreateMapPattern` also became an `ISolutionPattern`: `RewriteAsync` uses `MapIntoPattern.TargetsAsync` to get the pairs (A, B) that a `Map(a, b)` in the solution uses and writes `MapInto` only for those, so existing output stays the same. The `MapInto` methods come after all `ToB` methods (`GeneratedAsync` links method i to CreateMap i). `MapInto` returns `destination`, so a used result (`x = mapper.Map(a, b)`) keeps working.

## Requirements

- **REQ-1**: WHEN `Map(a, b)` has a visible `MapInto(this A, B)` THE SYSTEM SHALL replace the call with `a.MapInto(b)` (proof `MapInto/existing-method`).
- **REQ-2**: WHEN the profile with `CreateMap<A, B>` is rewritten and the solution contains `Map(a, b)` THE SYSTEM SHALL write a `MapInto` next to `ToB` and make the call `a.MapInto(b)`, with a `using` where needed (combined proof `map-into-profile`, unit test `FixAsync_ProfileInOtherFile_WritesMapIntoAndCallsItWithUsing`).
- **REQ-3**: IF source or destination is a collection THEN THE SYSTEM SHALL report Manual (proof `collection`).
- **REQ-4**: IF there is no rewritten `CreateMap<A, B>` and no `MapInto` THEN THE SYSTEM SHALL report Manual (proof `unknown-map`).
- **REQ-5**: IF the source can be null according to the nullable flow THEN THE SYSTEM SHALL report Manual (proof `maybe-null-source`).
- **REQ-6**: IF the destination can be null THEN THE SYSTEM SHALL report Manual; AutoMapper then creates a new object (proof `maybe-null-destination`).
- **REQ-7**: IF source or destination is `object`/`dynamic`, or the receiver can have side effects THEN THE SYSTEM SHALL report Manual (proofs `object-source`, `receiver-call`).
- **REQ-8**: WHEN no `Map(a, b)` uses the map THE SYSTEM SHALL not write a `MapInto` (existing CreateMap, ForMember and MapCall proofs unchanged).

## Assumptions

- AutoMapper picks the map by runtime type; here by static types.
- `MapInto` copies the same members as `ToB`; get-only members that the source has are already Manual in `CreateMapPattern`.

## Risks

- Every profile rewrite with a solution searches all files containing the text `Map` for `Map(a, b)`; slow for large solutions.
- A struct as destination: AutoMapper and `MapInto` both return a copy; same behaviour, but unusual.

## Models

Class diagram, sequence and decision tree: [automapper-map-into-modellen.md](automapper-map-into-modellen.md).

## Open questions

- None.
