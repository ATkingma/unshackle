---
kingmadoc: 1
feature: "automapper-project-to"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectToPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/Projections.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectionMember.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberValues.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/ProjectTo/
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/Core/ProjectToPatternTests.cs
---

# Feature: automapper.project-to: query.ProjectTo<B>(mapper.ConfigurationProvider) → query.Select(ProfileExtensions.AToBProjection)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-252.

## One-sentence summary

automapper.project-to: a `ProjectTo<B>` on an `IQueryable<A>` becomes `Select` with an `Expression<Func<A, B>>` that the profile rewrite writes next to `ToB`, with the member assignments AutoMapper 14's `ProjectTo` builds itself, so EF Core can still translate the query.

## Scope (in / out)

**In scope**

- `query.ProjectTo<B>(configuration)` (the `AutoMapper.QueryableExtensions` extension, any receiver chain) and `mapper.ProjectTo<B>(query)`, where `query` is an `IQueryable<A>` and the configuration or mapper is a plain field, property or variable (it is dropped).
- The `CreateMap<A, B>` in a profile one of the profile patterns rewrites. Its projection `AToBProjection` is written only when a `ProjectTo` call in the solution uses the map.
- Members: same type (read as is), `MapFrom` expressions (they already were expression trees for AutoMapper), a nested reference type with a map in the same profile (`source.X == null ? null : new Y { ... }`), and a list or array of such elements (`source.Xs.Select(x => new Y { ... }).ToList()` / `.ToArray()`), at any depth up to 8.

**Out of scope (Manual)**

- `ProjectTo` with parameters (`new { userId }`) or members to expand; a generic destination (`ProjectTo<TResult>` in a paging helper); a query that is no `IQueryable<A>` of a type of its own.
- A member that needs one of AutoMapper's conversions (nullable → value, number widening, enum → string, ...); a nested map of another profile; a value-type or `Nullable<T>` nested map; a cycle.
- Unknown maps, several profiles with the same map, a projection that becomes internal in another project.

## Design

Like `MapInto` (`automapper.map-into`), the projection is a by-product of the shared profile rewrite in `CreateMapPattern`, and the call rewrite is a pattern of its own. `ProjectToPattern.TargetsAsync` collects the type pairs of every rewritable `ProjectTo` call; `CreateMapPattern.RewriteAsync` passes them to `Rewrite`, which keeps, per map, what AutoMapper reads for each assigned member (`ProjectionMember`: `source.X` or the `MapFrom` body, with both types) next to the `ToB` values. After all maps of the profile are known, `Projections` turns those members into expression-tree-safe assignments, inlining nested maps of the same profile; a problem makes the whole profile Manual (the fix is all-or-nothing anyway). The projection is a property, not a method, so `MapCallPattern.GeneratedAsync` still pairs the i-th method with the i-th `CreateMap`.

Name: `AToBProjection` (e.g. `OrderToOrderDtoProjection`) rather than `ToBProjection`, because two maps to the same `B` in one profile would clash, and rather than a method, because `query.Select(X.AToBProjection)` reads as a value and EF Core needs the `Expression`, not a delegate.

`ProjectToPattern.Rewrite` finds an existing static `Expression<Func<A, B>> AToBProjection` (also the one the profile rewrite just wrote in the same file); `RewriteAsync` otherwise uses the profile found by `MapCallPattern.GeneratedAsync` and its own profile scan for the class name. The call becomes `query.Select(<Profile>Extensions.AToBProjection)` with `using System.Linq` and the profile's namespace. The `IMapper` stays, as for `automapper.map-call`.

## Requirements

- **REQ-1**: WHEN a profile map is used by a `ProjectTo` call THE SYSTEM SHALL write `public static Expression<Func<A, B>> AToBProjection => source => new B { ... }` after the `ToB`/`MapInto` methods, with `using System` and `using System.Linq.Expressions` (proof `CreateMap/project-to`).
- **REQ-2**: WHEN a member has the same type THE SYSTEM SHALL read it as is (`Years = source.Years`), like AutoMapper's projection (proof `CreateMap/project-to`).
- **REQ-3**: WHEN a member is a reference type with a map in the same profile THE SYSTEM SHALL inline it as `source.X == null ? null : new Y { ... }` (`null!` for a non-nullable member in a nullable context) (proofs `CreateMap/project-to`, `_Combined/project-to-query`).
- **REQ-4**: WHEN a member is a list or array of such elements THE SYSTEM SHALL write `source.Xs.Select(x => new Y { ... }).ToList()` (`ToArray()` for an array), without a null check (proofs `CreateMap/project-to`, `_Combined/project-to-query`).
- **REQ-5**: WHEN a `MapFrom` expression or an `Ignore()` is configured THE SYSTEM SHALL use the expression with `source` as parameter, and leave the ignored member out (proof `_Combined/project-to-query`).
- **REQ-6**: WHEN `query.ProjectTo<B>(cfg)` or `mapper.ProjectTo<B>(query)` uses a map with a projection THE SYSTEM SHALL rewrite it to `query.Select(Class.AToBProjection)`, keeping a chain's line breaks, adding the usings (proofs `ProjectTo/existing-projection`, `_Combined/project-to-query`, unit test `FixAsync_ProfileInOtherFile_WritesProjectionAndSelectsIt`).
- **REQ-7**: IF `ProjectTo` has members to expand or parameters other than `new { name, name = value }`, a generic destination, or no known map THEN THE SYSTEM SHALL report Manual (proofs `ProjectTo/parameters-generic`, `ProjectTo/generic`, `ProjectTo/unknown-map`, unit test `FixAsync_ParametersThatDoNotFitTheCapturedVariable_AreManual`).
- **REQ-8**: IF a member needs a conversion or a nested map of another profile THEN THE SYSTEM SHALL report Manual for the profile (proof `CreateMap/project-to-conversion`, unit test `FixAsync_NestedMapInOtherProfile_IsManual`).
- **REQ-9**: WHEN no `ProjectTo` call uses a map THE SYSTEM SHALL write no projection (unit test `FixAsync_NoProjectToCall_WritesNoProjection`; all earlier proofs unchanged).

- **REQ-10** (TAAK-265): WHEN a profile constructor declares `int userId = 0;`, a `MapFrom` reads it and `ProjectTo<B>(config, new { userId })` fills it THE SYSTEM SHALL write `AToBProjection(int userId = 0)` as a method (and `ToB(this A source, int userId = 0)`), and rewrite the call to `query.Select(Class.AToBProjection(userId))`; a call without parameters passes none (the default is the literal, which is what AutoMapper's `Map` and a parameterless `ProjectTo` use, measured) (proof `_Combined/projection-parameters`, unit test `FixAsync_CapturedVariable_ProjectionTakesItAsParameter`).
- **REQ-11** (TAAK-265): IF a parameter has a name the map does not read, another type than the variable, or the call passes anything but an anonymous object THEN THE SYSTEM SHALL report Manual (proof `ProjectTo/parameters`, unit test `FixAsync_ParametersThatDoNotFitTheCapturedVariable_AreManual`).

## Assumptions

- Measured with AutoMapper 14.0.0 (TAAK-252 **Evidence**): the projection of a plain map is `new B { X = src.X, Nested = src.Nested == null ? null : new C { ... }, List = src.List.Select(l => new D { ... }).ToList(), Array = ....ToArray(), Same = src.Same }`; a null list throws in memory in AutoMapper's projection too; `int` → `long` has no projection in AutoMapper (`Unable to create a map expression`), so a code base that runs has no such member in a projected map.
- `MapFrom` lambdas are `Expression<Func<...>>` for AutoMapper, so the body is already valid in an expression tree; the `ToB` rewrite has already refused bodies that dereference a maybe-null value.

- AutoMapper 14.0.0 (measured, TAAK-265): `Map` and `ProjectTo(config)` use the literal of `int userId = 0;`, `ProjectTo(config, new { userId = 2 })` replaces it in the projection (and in nested maps of the same profile), so `ToB()`/`AToBProjection()` with the default and `AToBProjection(userId)` are the same values. A parameter typed differently from the variable is not converted by this fix (Manual).

## Risks

- A nested map in another profile is Manual; in large Clean Architecture code bases (one profile per feature) this can keep many `ProjectTo` calls Manual.
- The `null!` hides that a non-nullable member can be null; it is what AutoMapper does too.

## Models

Classes, sequence and decision tree: [automapper-project-to-modellen.md](automapper-project-to-modellen.md).

## Open questions

- Inlining maps of other profiles (needs the member list of a profile in another document or project) is a follow-up.
