---
kingmadoc: 1
feature: "automapper-generic-map"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Generics/
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericMapCode.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericRequestPattern.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/GenericMap/
    - src/Unshackle.Tests/Core/AutoMapper/GenericMapPatternTests.cs
---

# Feature: automapper.generic-map: Map<T> on a type parameter → dispatch over the closed set

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-26 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-324 and council round 13, decision A11 (`claude/tasks/COUNCIL-round-13.md`).

## One-sentence summary

`mapper.Map<TDto>(entity)` where the source or destination is a type parameter of the method or type around it becomes
`typeof(TDto) == typeof(OrderDto) ? (TDto)(object)entity.ToOrderDto() : … : throw`, over the closed type arguments that every
caller, use and derived type in the solution provably passes; otherwise Manual `GenericMapOpenSet`, naming the unresolved caller.

## Scope (in / out)

**In scope**

- `Map<B>(a)` / `Map<A, B>(a)` (one argument) whose source or destination is exactly a type parameter of the enclosing method,
  of the type that declares it, or both (generic repository and service bases).
- Closure through generic code: a caller that is itself generic is closed in turn, up to depth 3.
- Closed type arguments that are plain non-generic types (or special types) visible from the project of the `Map` call.

**Out of scope (Manual `GenericMapOpenSet`)**

- A member reachable from outside the solution (R1: visible outside a packable project; R2: internal, but `InternalsVisibleTo`
  names an assembly outside the solution).
- A virtual, abstract, override or interface method (callers through the base or interface are not followed), a method group,
  a reduced extension method, a dynamic call, `MakeGenericMethod`/`MakeGenericType`, or `typeof(G<>)` (open generic registration).
- A type parameter inside another generic type (`Map<List<T>>`, `Wrapper<T>`), a collection, a nested generic type, a local function.
- An empty set, a chain deeper than 3, a closed pair without a generated (or existing) map, a closed type not visible from the call's project.
- `ProjectTo<T>`: A12, TAAK-325 (`automapper.generic-destination`).

## Design

The closure is a shared Core service (`src/Unshackle.Core/Generics/`), so MediatR and AutoMapper use one collection of generic uses:

- `GenericUses` — extracted from MediatR's `GenericRequestPattern.Scan` (TAAK-193): every written `G<...>` in all compilations, classified
  as `Closed`, `SubclassBase`, `Open`, `OpenCreation` or `Unbound`, plus the cross-compilation helpers (`MetadataKey`, `ArgumentsKey`,
  `ContainsTypeParameter`, `CompilationsAsync`). A per-compilation index of simple names keeps repeated closures from walking the solution again.
  `GenericRequestPattern` now builds its `GenericRequestUses` from it (behaviour unchanged).
- `TypeArgumentClosure.Close(parameters)` — for a generic method: every invocation of it (type arguments of the method and of its
  containing type); for a generic type: every written use (including inside its own declaration, base classes of derived types).
  A use whose arguments are type parameters of the code around it is closed in turn (`depth + 1`). The R1/R2 and reflection checks
  run per level. Constraints (class/struct, base/interface constraints without type parameters) filter the result (A11 option B).

`GenericMapPattern` (registered after `MapCallPattern`; `MapCallPattern.Find` leaves these calls to it) uses `MapCallPattern.Check` for the
call shape and nullability and `MapCallPattern.SingleAsync` per closed pair, so each branch calls exactly the method the plain
`map-call` would call (existing `ToB()` or the one the profile rewrite generates). The receiver is cast `(Order)(object)entity` when the
source is a type parameter; the value `(TDto)(object)(…)` when the destination is one. The final `throw` is only reached by a type argument
outside the proven set, where AutoMapper also throws (missing type map).

The partial plan (`MapOwners`, TAAK-272) runs this pattern on the generic calls too: a Fixed call no longer keeps "every map" on AutoMapper;
a Manual one still does (as before), and its kept result carries the code `GenericMapOpenSet`.

## Requirements

- **REQ-1**: WHEN a generic method's type parameter is closed by every caller THE SYSTEM SHALL dispatch over the closed types (proof `two-callers`).
- **REQ-2**: WHEN a generic base type's type parameters are closed by derived types THE SYSTEM SHALL dispatch over the closed pairs, casting the source (proof `derived-repositories`).
- **REQ-3**: WHEN a type parameter is closed through generic code THE SYSTEM SHALL follow the chain up to depth 3, and respect constraints (proof `constraint-filter`; unit test `FixAsync_ChainDeeperThanLimit_IsOpenSet`).
- **REQ-4**: IF the member is public in a packable project (R1) or internal with `InternalsVisibleTo` to an assembly outside the solution (R2) THEN Manual `GenericMapOpenSet` (proof `packable-public-base`; unit tests `FixAsync_CallersNotAllKnown_IsOpenSet`, `FixAsync_UnprovableCaller_NamesIt`); a public member of an executable is closed (`FixAsync_PublicMethodInExecutable_IsNotReachableFromOutside`).
- **REQ-5**: IF `typeof(G<>)` is used THEN Manual naming the place (proof `open-registration`).
- **REQ-6**: IF `MakeGenericMethod`/`MakeGenericType` is called THEN Manual naming the place (proof `reflection-caller`).
- **REQ-7**: IF the method is virtual/override/interface, used as a method group, or called dynamically THEN Manual naming the place (unit tests).
- **REQ-8**: IF a closed pair has no generated or existing map THEN Manual naming the pair (`FixAsync_PairWithoutMap_IsOpenSet`).
- **REQ-9**: IF a closed type is not visible from the call's project THEN Manual (`FixAsync_ClosedTypeInDependentProject_IsOpenSet`).
- **REQ-10**: WHEN the closure is extracted THE SYSTEM SHALL keep `mediatr.generic-request` unchanged (its proofs and `GenericRequestPatternTests` stay green).

## Assumptions

- Every closed `C<X>` of a generic type is written somewhere in the solution (construction, base class, field/parameter type) unless it is
  made by reflection, which needs `typeof(C<>)` or `MakeGenericType`; both make the set open.
- Packability is guessed as in MediatR's `AssemblyResolver` without project properties (a library without a test-framework reference is packable),
  because `AutoMapperFix` has no `IProjectProperties`: a library with `IsPackable=false` still counts as packable (Manual rather than a wrong Fixed).

## Risks

- Over-approximation: a written `Repo<Invoice>` that never maps adds a pair; without its map the call stays Manual (safe, fewer Fixed).
- A null source of type parameter type follows the nullable flow as `map-call` does; without a nullable context it is treated as not null.
- The dispatch is longer than the call; it is formatted one branch per line.

## Models

Classes, sequence and decision tree: [automapper-generic-map-modellen.md](automapper-generic-map-modellen.md).

## See also

- [automapper-generic-map-verify.md](automapper-generic-map-verify.md)
- [automapper-map-call-plan.md](automapper-map-call-plan.md)
- [mediatr-generic-request-plan.md](mediatr-generic-request-plan.md)
- `claude/tasks/COUNCIL-round-13.md` (A11)
