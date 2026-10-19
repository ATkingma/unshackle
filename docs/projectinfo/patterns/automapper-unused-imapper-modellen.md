# Models: automapper.unused-imapper

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnusedMapperPattern.cs`, `MapperCleanup.cs`, `MapperRegistration.cs` and `AddAutoMapperPattern.cs`.

## Figure 1: class diagram

<!-- kingmadoc:diagram img/automapper-unused-imapper-modellen-1.d2 -->
![Figure 1: class diagram](img/automapper-unused-imapper-modellen-1.png)

| Element                | Role                                                                                           |
| ---------------------- | ---------------------------------------------------------------------------------------------- |
| `UnusedMapperPattern`  | finds the types with removals and rewrites each in one go                                      |
| `MapperCleanup`        | the solution-wide plan: which IMapper symbols go, and which nodes that removes                 |
| `Removal`              | one node to remove or trim, with an optional warning (removed DI lookup or its null check)     |
| `MapperRegistration`   | the scope rule of council decision 3: is IMapper surely registered for a project's code (TAAK-292) |
| `MapOwners`            | tells which Map calls stay on AutoMapper (`KeptCall`); its cache check is shared               |
| `AddAutoMapperPattern` | skips an IMapper reference that lies inside a removal                                          |

## Figure 2: sequence

<!-- kingmadoc:diagram img/automapper-unused-imapper-modellen-2.d2 -->
![Figure 2: sequence](img/automapper-unused-imapper-modellen-2.png)

## Figure 3: decision tree (per candidate, per round of the fixed point)

<!-- kingmadoc:diagram img/automapper-unused-imapper-modellen-3.d2 -->
![Figure 3: decision tree (per candidate, per round of the fixed point)](img/automapper-unused-imapper-modellen-3.png)

| Question                         | Leaf                     | Proof / test                                                                 |
| -------------------------------- | ------------------------ | ---------------------------------------------------------------------------- |
| only assigned                    | removed                  | `assigned-only`                                                              |
| uses are migrated calls          | removed, registration too | `_Combined/unused-imapper-registration`                                     |
| caller passes a factory value    | removed with factory     | `test-factory`                                                               |
| null checks on the parameter     | removed                  | `null-checks`                                                                |
| DI lookup with `== null` check, scope registered | removed, with a warning naming AddAutoMapper | `service-lookup`, `FixAsync_ServiceLookupInTheRegisteringProject_…`, `FixAsync_ServiceLookupRegisteredByADependentHost_RemovesIt` |
| handed out, `null` passed, clash | stays                    | `FixAsync_MapperUsedOtherwise_KeepsItAndTheRegistration`                     |
| lookup, scope not registered     | stays, Manual names the rule | `service-lookup-conditional-registration`, `service-lookup-second-container`, `FixAsync_ServiceLookupNotSurelyRegistered_KeepsTheLookupAndNamesTheRule` |
| constructor or class left empty  | kept, Info line          | `empty-factory-class`, `FixAsync_FactoryClassLeftEmpty_KeepsItAndReportsAnInfoLine` |

## Figure 4: scope rule for a `GetService<IMapper>()` lookup (council decision 3)

<!-- kingmadoc:diagram img/automapper-unused-imapper-modellen-4.d2 -->
![Figure 4: scope rule for a GetService<IMapper>() lookup](img/automapper-unused-imapper-modellen-4.png)

| Question | Leaf | Proof / test |
| -------- | ---- | ------------ |
| rule 1 fails (no AddAutoMapper in the scope) | stays, Manual "rule 1" | `FixAsync_ServiceLookupNotSurelyRegistered_…("")`, `("in shop")` |
| rule 2 fails (only conditional calls) | stays, Manual "rule 2" | `service-lookup-conditional-registration`; theory rows `if`, `#if` |
| rule 3 fails (second container) | stays, Manual "rule 3" | `service-lookup-second-container`; theory row `container` |
| all hold | removed, warning | `service-lookup`, `FixAsync_ServiceLookupRegisteredByADependentHost_RemovesIt` |

The scope is the lookup's project plus `GetProjectsThatTransitivelyDependOnThisProject`. The kept lookups are found by planning twice:
once with the scope rule, once as if every project were registered; a symbol only the second plan removes is a lookup the rule keeps.

## See also

- [plan](automapper-unused-imapper-plan.md), [verify](automapper-unused-imapper-verify.md),
  `src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnusedMapperPattern.cs`, `src/Unshackle.Tests/Proofs/AutoMapper/UnusedMapper/`
