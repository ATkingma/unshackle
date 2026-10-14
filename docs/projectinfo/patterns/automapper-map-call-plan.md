---
kingmadoc: 1
feature: "automapper-map-call"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/MapCall/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
---

# Feature: automapper.map-call: mapper.Map<B>(a) → a.ToB()

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Project**   | Unshackle                                                         |
| **Status**    | Approved                                                          |
| **Generated** | 2026-10-12 by KingmaDoc (by hand, `kingmadoc` not available)      |

> Drawn up from TAAK-157.

## One-sentence summary

automapper.map-call: `mapper.Map<B>(a)` and `mapper.Map<A, B>(a)` become `a.ToB()`, the extension method that already exists or that `automapper.create-map` (or a ForMember pattern) writes for `CreateMap<A, B>`.

## Scope (in / out)

**In scope**

- AutoMapper's `Map<B>(a)` and `Map<A, B>(a)` (one argument), called on a name, `this.x` or a chain of names.
- A `ToB` that the calling code already sees, or that is in the project's source code (then a `using` for the namespace).
- `CreateMap<A, B>` in a profile that the profile patterns rewrite, in the same project or a project it depends on.

**Out of scope (Manual or another pattern)**

- `Map(src, dest)`, `Map` with options and `ProjectTo`: stay Manual in `CreateMapPattern` (rows 5 and 7).
- Collections (`Map<List<B>>(as)`), source `object`/`dynamic`, a source that can be null according to the nullable flow.
- No profile or a Manual profile, several profiles with the same map, an internal method in another project.
- Removing the `IMapper` injection: it stays (see Design).

## Design

`MapCallPattern` is an `ISolutionPattern`. The Fixer fixes every file against the original solution: in another file `ToB` does not exist yet for the semantic model. The pattern therefore first looks for an existing `ToB` (in scope, or in the compilation's source code; that also covers the profile in the same file, which `CreateMapPattern` has then already rewritten) and otherwise in the solution for every profile with `CreateMap<A, B>`. For that it calls `CreateMapPattern.Rewrite` and takes the method name and accessibility from the generated class, so they stay equal to what the fix really writes.

`CreateMapPattern.Find` leaves alone the calls that `MapCallPattern.IsMapCall` recognises; the rest of `Map`/`ProjectTo` stays Manual and with `AllOrNothing` holds back the whole fix.

`IMapper` stays injected: removing a constructor parameter changes every caller (DI, tests with `new Service(mapper)`). Cleaning up belongs to row 6 (`add-automapper`), when the registration disappears too.

## Requirements

- **REQ-1**: WHEN `Map<B>(a)` or `Map<A, B>(a)` has a visible `ToB()` THE SYSTEM SHALL replace the call with `a.ToB()`, with parentheses where needed (proof `existing-method`).
- **REQ-2**: WHEN the profile with `CreateMap<A, B>` is rewritten THE SYSTEM SHALL write `a.ToB()` and import the namespace of the extension class (combined proofs `map-call-profile`, `map-call-global`, unit test `FixAsync_ProfileInOtherFile_CallsGeneratedMethodWithUsing`).
- **REQ-3**: WHEN the profile is in a project the calling project depends on THE SYSTEM SHALL call the public method and report an internal method as Manual (unit test `FixAsync_ProfileInReferencedProject_FollowsAccessibility`).
- **REQ-4**: IF source or destination is a collection THEN THE SYSTEM SHALL report Manual (proof `collection`). *Superseded by REQ-9 (TAAK-259).*
- **REQ-5**: IF there is no rewritten `CreateMap<A, B>` and no `ToB` THEN THE SYSTEM SHALL report Manual (proofs `unknown-map`, `manual-profile`).
- **REQ-6**: IF the source can be null according to the nullable flow THEN THE SYSTEM SHALL report Manual (proof `maybe-null`). *Narrowed by REQ-10 (TAAK-259): only where the target does not allow null.*
- **REQ-7**: IF the source is `object` or `dynamic`, or the receiver can have side effects THEN THE SYSTEM SHALL report Manual (proofs `object-source`, `receiver-call`).
- **REQ-9**: WHEN `Map<List<B>>`, `Map<B[]>`, `Map<IEnumerable<B>>`, `Map<IList<B>>` (or another interface AutoMapper fills with a `List<T>`) maps a collection of A and A→B is rewritten THE SYSTEM SHALL write `items?.Select(x => x?.ToB()).ToList() ?? []` (`.ToArray()` for an array), with the collection logic of nested members (`MemberValues`); without `?.`/`?? []` when the source is not null by the nullable flow, and without `x?.` when the elements are declared non-null. Measured with AutoMapper 14.0.0: a null source gives an empty collection, a null element a null one, interfaces get a `List<T>` (combined proofs `collection-map-call`, `collection-nullable-map-call`, unit test `FixAsync_CollectionWithProfileInOtherFile_SelectsGeneratedMethod`). Other collections (sets, dictionaries) stay Manual.
- **REQ-10**: WHEN the source may be null and the target allows null (`Map<B?>`, or a return, explicitly typed local, field, property or assignment declared `B?`) THE SYSTEM SHALL write `a?.ToB()` (measured: AutoMapper 14 returns null for a null source); a `var` local, a non-nullable target or a struct destination stays Manual (proofs `maybe-null-target`, `maybe-null`, unit test `FixAsync_MaybeNullSource_ConditionalOnlyWhereNullAllowed`).
- **REQ-8**: WHEN `Map` with options or `ProjectTo` occurs THE SYSTEM SHALL leave it Manual in `CreateMapPattern` (no own proof any more: `CreateMap/map-into` was removed since `Map(src, dest)` went to `automapper.map-into`, see [automapper-map-into-plan.md](automapper-map-into-plan.md); updated in TAAK-159).

## Assumptions

- For `Map<B>(object)` AutoMapper chooses the map by the runtime type; here the static type of `a` is the source. A derived type with its own map is mapped differently after the fix.
- Without a nullable context the flow state is `None`: then it is rewritten.

## Risks

- `ToB` throws a NullReferenceException on `null` where AutoMapper gave `null`; code without a nullable context that passes `null` only notices at runtime.
- Per call the solution is searched for profiles (only files with the text `CreateMap`); slow with many calls.

## Models

Class diagram, sequence and decision tree: [automapper-map-call-modellen.md](automapper-map-call-modellen.md).

## Open questions

- Unused `IMapper` injection: include it with row 6.
