# Models: automapper.generic-destination

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericDestinationPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `GenericDestinationPattern` | `ISolutionPattern`: finds `ProjectTo<T>` on a type parameter (`IsGenericCall`), builds the selector |
| `GenericDestinationCode` | Reason code `GenericProjectionOpenSet` |
| `ProjectToPattern` | Leaves generic calls to this pattern; `ProjectionAsync`, `Fixed`, `Element` are shared (internal) |
| `GenericMapPattern.Local` | Maps a closed type into the compilation of the call |
| `MapOwners` | The partial plan runs this pattern on generic `ProjectTo` calls too |
| `TypeArgumentClosure` (Core) | Closes the type parameters (A11); now follows reduced extension calls |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` / `IsGenericCall` | `ProjectToPattern.IsProjectToCall` with a type-parameter destination |
| Shape | `DispatchAsync` | One argument, plain configuration/mapper, query element plain or a type parameter |
| Close | `TypeArgumentClosure.Close` | Source and destination type parameters together |
| Map types | `GenericMapPattern.Local` | Each closed type visible from the call |
| Per pair | `ProjectToPattern.ProjectionAsync(requireTarget: true)` | Existing projection, or the generated one of a non-generic `ProjectTo` of the pair |
| Rewrite | `ProjectToPattern.Fixed` | `query.Select(typeof(T) == typeof(X) ? (Expression<Func<S, T>>)(object)P : … : throw)` |
| Plan | `RewriteAsync` | `MapOwners.KeptCallAsync` first result wins; a kept open set carries `GenericProjectionOpenSet` |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Fixed: destination type parameter | every caller closes it, each pair has a projection | `two-callers`, `FixAsync_ProfileWithPlainProjections_SelectsGeneratedProjectionByType` |
| Fixed: source and destination type parameters | extension-method callers | `paging-helper` |
| Manual: packable public | R1 | `packable-paging-helper`, `FixAsync_PublicInPackableLibrary_IsOpenSet` |
| Manual: pair without Fixed projection | `ProjectionAsync` problem | `no-projection`, `FixAsync_PairWithoutPlainProjectTo_IsOpenSet` |
| Manual: parameters / members to expand | argument count ≠ 1 | `parameters` |
| Manual: other A11 leaves | closure unresolved | see [automapper-generic-map-modellen.md](automapper-generic-map-modellen.md) |

## See also

- [automapper-generic-destination-plan.md](automapper-generic-destination-plan.md)
- [automapper-generic-destination-verify.md](automapper-generic-destination-verify.md)
- `src/Unshackle.Tests/Proofs/AutoMapper/GenericDestination/`
