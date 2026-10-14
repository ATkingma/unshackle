# Models: automapper.project-to

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectToPattern.cs`, `Projections.cs`, `ProjectionMember.cs` and `CreateMapPattern.cs`.

> Written by hand as tables; `kingmadoc`/`d2` were not available in this environment.

## Figure 1: classes

| Element             | Role                                                                                                                                      |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `ProjectToPattern`  | `ISolutionPattern` `automapper.project-to`: finds every AutoMapper `ProjectTo`, checks the form, rewrites the call to `Select`; `TargetsAsync` gives the maps that need a projection |
| `CreateMapPattern`  | Shared profile rewrite; `Rewrite(..., projectTo)` keeps a `ProjectionMember` list per map and writes `AToBProjection` in `Extensions`; its `Find` leaves `ProjectTo` calls to `ProjectToPattern` |
| `ProjectionMember`  | Model: member name, the value AutoMapper reads (`source.X` or the `MapFrom` body), source and destination type                             |
| `Projections`       | `Problem(key, out members)`: the expression-tree assignments of one map, inlining nested maps of the same profile; `Name(A, B)` = `AToBProjection` |
| `NestedMaps`        | Used by `Projections` only to recognise a map of another profile (Manual)                                                                  |
| `MemberValues`      | `TryElement` (now internal) gives the element type of a list or array                                                                      |
| `MapCallPattern`    | `GeneratedAsync` confirms the profile is rewritable and gives its namespace, internal flag and project                                     |

## Figure 2: sequence

| Step        | Method in the code                                                | What happens                                                                 |
| ----------- | ----------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Targets     | `ProjectToPattern.TargetsAsync` → `Check`                         | Type pair `(A, B)` of every rewritable `ProjectTo` call in the solution       |
| Profile     | `CreateMapPattern.RewriteAsync` → `Rewrite` → `Problem`/`MapFromProblem` | `ToB` values plus the `ProjectionMember` list per map                        |
| Projection  | `Projections.Problem` → `Value` → `Inline`                        | Assignments per member; Manual stops the profile                              |
| Write       | `CreateMapPattern.Extensions`                                     | `AToBProjection` properties after the methods; usings `System`, `System.Linq.Expressions` (+ `System.Linq`) |
| Call        | `ProjectToPattern.RewriteAsync` → `Existing` / `GeneratedAsync` + `ProfilesAsync` → `Fixed` | `query.Select(<Profile>Extensions.AToBProjection)`, usings `System.Linq` and the profile namespace |

## Figure 3: decision tree (one member, `Projections.Value`)

1. Both collections (list family or array) and the elements have a map in this profile → `Xs.Select(x => new Y { ... }).ToList()` / `.ToArray()` (cycle → Manual).
2. Elements mapped by another profile → Manual.
3. Both reference types with a map in this profile → `X == null ? null : new Y { ... }` (cycle → Manual).
4. Mapped by another profile → Manual.
5. Same type → read as is.
6. Otherwise (a conversion) → Manual.

## Figure 4: decision tree (one call, `ProjectToPattern.Check`)

1. Not bound to one AutoMapper `ProjectTo` called as `x.ProjectTo<B>(...)` → Manual.
2. More than two arguments (members to expand), or a second argument that is no `new { name, name = value }` → Manual. The anonymous members are kept as `parameters` (TAAK-265).
3. Dropped configuration or mapper with side effects → Manual.
4. `B` a type parameter, `object` or `dynamic` → Manual.
5. Query not an `IQueryable<A>` of a type of its own → Manual.
6. Existing `AToBProjection` (property, or method with parameters) → `Select`; else one rewritable profile with the map → `Select`; else Manual (several profiles: `MapBlockers.Several`, "the one registered last").
7. Parameters (`Arguments`): each name must be a parameter of the generated `AToBProjection(...)` method (read back from `CreateMapPattern.RewriteProjectionAsync`) with the same type, else Manual; the call is `Class.AToBProjection(args)`, positional when the call passes the leading parameters in order, else named; unnamed parameters keep their default.
