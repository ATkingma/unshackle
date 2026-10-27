---
kingmadoc: 1
feature: "automapper-generic-destination"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericDestinationPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericDestinationCode.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectToPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Core/Generics/TypeArgumentClosure.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/GenericDestination/
    - src/Unshackle.Tests/Core/AutoMapper/GenericDestinationPatternTests.cs
    - src/Unshackle.Tests/Core/AutoMapper/GenericDestinationTranslationTests.cs
---

# Feature: automapper.generic-destination: ProjectTo<T> on a type parameter → a selector chosen by typeof

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-27 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-325 and council round 13, decision A12 (`claude/tasks/COUNCIL-round-13.md`).

## One-sentence summary

`query.ProjectTo<TResult>(configuration)` where `TResult` is a type parameter of the method or type around it becomes
`query.Select(typeof(TResult) == typeof(OrderDto) ? (Expression<Func<Order, TResult>>)(object)OrderMapping.OrderToOrderDtoProjection : … : throw)`,
over the closed type arguments that every caller provably passes and only when each closed pair already has a Fixed projection;
otherwise Manual `GenericProjectionOpenSet`.

## Scope (in / out)

**In scope**

- `query.ProjectTo<T>(configuration)` and `mapper.ProjectTo<T>(query)` with one argument, whose destination is exactly a type parameter.
- A query whose element type is a plain type or itself a type parameter (the cats `ProjectToPaginatedDataAsync` shape: `IQueryable<T>` → `TResult`).
- Closure as in A11 (`TypeArgumentClosure`: callers, uses, derived types, depth 3, R1/R2, reflection), including callers that call the
  helper as an extension method (`orders.ProjectToPage<Order, OrderDto>(...)`).
- Per closed pair, the projection `ProjectToPattern` would use: one the code already has, or the `AToBProjection` the profile rewrite writes
  because a non-generic `ProjectTo` of the same pair exists in the solution.

**Out of scope (Manual `GenericProjectionOpenSet`)**

- Everything A11 keeps open (R1/R2, virtual/interface, method group, dynamic, reflection, `typeof(G<>)`, depth > 3, empty set).
- `ProjectTo<T>` with parameters or members to expand (Kavita's `ProjectToWithProgress`). Council round 16 A18: stays Manual; only one
  repository needs it and the reason is clear, so no follow-up until the corpus shows more.
- A closed pair without a Fixed projection (no existing `AToBProjection`, no non-generic `ProjectTo` of the pair, or the pair's projection is Manual).
  Council round 16 A16: the closure is not fed into the projection pass; that couples two passes and could write projections nobody
  asked for. Revisit with the corpus numbers.
- A query element that contains a type parameter inside another generic type, or is `object`/`dynamic`.
- A closed type argument not visible from the call's project.

## Design

`GenericDestinationPattern` (registered after `ProjectToPattern`; `ProjectToPattern.Find` leaves these calls to it) reads the query and
the dropped configuration like `ProjectToPattern.Check`, closes the type parameters with `TypeArgumentClosure` and asks
`ProjectToPattern.ProjectionAsync(..., requireTarget: true)` for the projection of each closed pair. `ProjectionAsync` is the part of
`ProjectToPattern.RewriteAsync` after the call check, extracted so both patterns choose the same projection (A12: "each member already has a
Fixed projection").

The rewrite replaces `ProjectTo<T>(…)` by `Select(selector)` with `ProjectToPattern.Fixed`. The selector is a conditional expression over
`typeof(...)` comparisons: C# evaluates it before `Queryable.Select` is called, so the expression tree EF Core receives is exactly the closed
projection (A12: "selects a closed Expression by typeof before building the query, never inside it"). `GenericDestinationTranslationTests`
shows `ToQueryString()` on SQLite equal to the closed rewrite's. The final `throw` is only reached by a type argument outside the proven set,
where AutoMapper also throws (missing map).

The task text suggested a private static selector member; the inline conditional has the same evaluation order (outside the query) and
needs no member insertion from an invocation rewrite. See the council question in TAAK-325.

The partial plan (`MapOwners`) runs this pattern on the generic calls: a Fixed call no longer keeps its maps on AutoMapper; a Manual one does,
and its kept result carries `GenericProjectionOpenSet`.

`TypeArgumentClosure` now follows a call to a generic extension method in reduced form (`x.M<A, B>()`): its id differed from the
declaration's, so such callers were skipped without a failure. The constructed unreduced method carries the same type arguments.

## Requirements

- **REQ-1**: WHEN a generic method's destination type parameter is closed by every caller and each pair has a projection THE SYSTEM SHALL select it by typeof (proof `two-callers`, unit test `FixAsync_ProfileWithPlainProjections_SelectsGeneratedProjectionByType`).
- **REQ-2**: WHEN source and destination are both type parameters and callers close them through an extension call THE SYSTEM SHALL dispatch on both (proof `paging-helper`).
- **REQ-3**: THE SYSTEM SHALL produce a selector whose translation equals the closed rewrite's (`GenericDestinationTranslationTests.ToQueryString_GenericSelector_EqualsClosedProjection`), and throw outside the set (`Page_TypeOutsideClosedSet_Throws`).
- **REQ-4**: IF the member is reachable from outside the solution THEN Manual `GenericProjectionOpenSet` (proof `packable-paging-helper`, `FixAsync_PublicInPackableLibrary_IsOpenSet`).
- **REQ-5**: IF a closed pair has no Fixed projection THEN Manual naming the pair (proof `no-projection`, `FixAsync_PairWithoutPlainProjectTo_IsOpenSet`).
- **REQ-6**: IF `ProjectTo<T>` has parameters or members to expand THEN Manual (proof `parameters`).
- **REQ-7**: WHEN a ProjectTo has a destination of its own THE SYSTEM SHALL leave it to `automapper.project-to` unchanged (`Find_GenericProjectTo_IsNotProjectToPatterns`, `Proofs/AutoMapper/ProjectTo/*`).
- **REQ-8**: WHEN the closure follows reduced extension calls THE SYSTEM SHALL keep `automapper.generic-map` and `mediatr.generic-request` proofs green.

## Assumptions

- A projection is only "Fixed" for a pair when the code has it or a non-generic `ProjectTo` of that pair makes the profile write it; a generic
  call does not make the profile write new projections (no feedback from closure into `ProjectToPattern.TargetsAsync`).
- Packability as in A11 (`IProjectProperties` when wired, else the guess that errs towards Manual).

## Risks

- Corpus delta is expected to be low: TAAK-332 found every generic map in a public member of an SDK-default packable library (R1).
- The selector is longer than the call; it is formatted one branch per line.

## Models

Classes, sequence and decision tree: [automapper-generic-destination-modellen.md](automapper-generic-destination-modellen.md).

## See also

- [automapper-generic-destination-verify.md](automapper-generic-destination-verify.md)
- [automapper-project-to-plan.md](automapper-project-to-plan.md)
- [automapper-generic-map-plan.md](automapper-generic-map-plan.md)
- `claude/tasks/COUNCIL-round-13.md` (A12)
