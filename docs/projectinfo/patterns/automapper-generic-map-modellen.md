# Models: automapper.generic-map

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericMapPattern.cs` and `src/Unshackle.Core/Generics/`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `GenericMapPattern` | `ISolutionPattern`: finds `Map` calls on a type parameter (`IsGenericCall`), builds the dispatch |
| `GenericMapCode` | Reason code `GenericMapOpenSet` |
| `MapCallPattern` | Leaves generic calls to this pattern; `Check` and `SingleAsync` are shared (internal) |
| `MapOwners` | The partial plan runs this pattern on generic calls too |
| `TypeArgumentClosure` (Core) | Closes type parameters over callers/uses/derived types, depth 3, R1/R2, reflection, constraints |
| `ClosedSets` (Core) | Result: the sets, or `Unresolved` with the reason |
| `GenericUses`, `GenericUse`, `GenericUseKind` (Core) | Written uses of a generic type across compilations (extracted from TAAK-193) |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` / `IsGenericCall` | `MapCallPattern.IsMapCall` and a source or destination containing a type parameter |
| Shape | `MapCallPattern.Check` | Receiver, object/dynamic source, collection, nullability kind |
| Close | `TypeArgumentClosure.Close` | `CloseByCalls` (generic method) or `CloseByUses` (generic type), `Resolve` recursion, `Satisfies` filter |
| Map types | `Local` | Each closed type into the compilation of the call (plain, visible) |
| Per pair | `MapCallPattern.SingleAsync` | The `ToB()` the plain call would get, on the cast receiver |
| Rewrite | `DispatchAsync` | `typeof(T) == typeof(X) ? … : throw`, one branch per line |
| Plan | `RewriteAsync` | `MapOwners.KeptCallAsync` first result wins; a kept open set carries `GenericMapOpenSet` |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Fixed: dispatch on a method type parameter | every caller closes it | `two-callers`, `FixAsync_ProfileAndTwoCallers_DispatchesToGeneratedMethods` |
| Fixed: dispatch on type parameters of a base | derived types close them | `derived-repositories`, `Close_TypeParameterOfClass_CollectsUsesAndDerivedTypes` |
| Fixed: through a generic chain | depth ≤ 3, constraints hold | `constraint-filter` |
| Manual: packable public | R1 | `packable-public-base`, `FixAsync_CallersNotAllKnown_IsOpenSet` |
| Manual: friend outside the solution | R2 | `FixAsync_UnprovableCaller_NamesIt` |
| Manual: `typeof(G<>)` | open registration | `open-registration` |
| Manual: reflection | `MakeGenericMethod`/`MakeGenericType` | `reflection-caller` |
| Manual: virtual / method group / dynamic | callers not followed | `FixAsync_CallersNotAllKnown_IsOpenSet`, `FixAsync_UnprovableCaller_NamesIt` |
| Manual: deeper than 3 | chain | `FixAsync_ChainDeeperThanLimit_IsOpenSet` |
| Manual: pair without map | `SingleAsync` Manual | `FixAsync_PairWithoutMap_IsOpenSet` |
| Manual: type not visible | `Local` null | `FixAsync_ClosedTypeInDependentProject_IsOpenSet` |

## See also

- [automapper-generic-map-plan.md](automapper-generic-map-plan.md)
- [automapper-generic-map-verify.md](automapper-generic-map-verify.md)
- `src/Unshackle.Tests/Proofs/AutoMapper/GenericMap/`
