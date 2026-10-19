# Models: automapper.unused-imapper

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnusedMapperPattern.cs`, `MapperCleanup.cs` and `AddAutoMapperPattern.cs`.

## Figure 1: class diagram

```d2
ISolutionPattern: {shape: class; "+RewriteAsync(node, model, solution, ct)": "Task<PatternResult>"}
ISolutionFinder: {shape: class; "+FindAsync(root, model, solution, ct)": "Task<IReadOnlyList<SyntaxNode>>"}
UnusedMapperPattern: {shape: class; "+FindAsync()": "outermost types with removals"; "+RewriteAsync()": "Task<PatternResult>"; "-Replacement()": "SyntaxNode?"; "-Remove()": "SyntaxNode"}
MapperCleanup: {shape: class; "+ForAsync(solution, model, ct)": "Task<MapperCleanup>"; "+Removals(root, model)": "IReadOnlyList<Removal>"; "+Removes(node, model)": "bool"; "+IsRemoved(declaration, model)": "bool"; "+RemovedArguments(arguments, model)": "IReadOnlyList<ArgumentSyntax>"; "-BuildAsync()": "fixed point"}
Removal: {shape: class; "+Node": "SyntaxNode"; "+Warning": "string?"}
MapOwners: {shape: class; "+ForAsync()": "Task<MapOwners>"; "+KeptCall(call, model)": "PatternResult?"; "+SameApartFrom(solution, cached, paths)": "bool"}
AddAutoMapperPattern: {shape: class; "+RewriteAsync()": "Task<PatternResult>"}
UnusedMapperPattern -> ISolutionPattern: implements
UnusedMapperPattern -> ISolutionFinder: implements
UnusedMapperPattern -> MapperCleanup: plan
MapperCleanup -> Removal: yields
MapperCleanup -> MapOwners: kept calls
AddAutoMapperPattern -> MapperCleanup: "Removes(): ignore uses"
```

| Element                | Role                                                                                           |
| ---------------------- | ---------------------------------------------------------------------------------------------- |
| `UnusedMapperPattern`  | finds the types with removals and rewrites each in one go                                      |
| `MapperCleanup`        | the solution-wide plan: which IMapper symbols go, and which nodes that removes                 |
| `Removal`              | one node to remove or trim, with an optional warning (dropped null check on a DI lookup)       |
| `MapOwners`            | tells which Map calls stay on AutoMapper (`KeptCall`); its cache check is shared               |
| `AddAutoMapperPattern` | skips an IMapper reference that lies inside a removal                                          |

## Figure 2: sequence

```d2
shape: sequence_diagram
Fixer
Pattern: UnusedMapperPattern
Cleanup: MapperCleanup
Owners: MapOwners
AddAutoMapper: AddAutoMapperPattern
Fixer -> Pattern: FindAsync(root, model, solution, ct)
Pattern -> Cleanup: ForAsync(solution, model, ct)
Cleanup -> Owners: ForAsync / KeptCall(call, model)
Cleanup -> Cleanup: BuildAsync: candidates, FindReferencesAsync, fixed point
Pattern -> Cleanup: Removals(root, model)
Fixer -> Pattern: RewriteAsync(type, model, solution, ct)
Pattern -> Pattern: "Replacement() / Remove() per removal"
Pattern -> Fixer: "Fixed(type) [Warning]"
Fixer -> AddAutoMapper: RewriteAsync(statement)
AddAutoMapper -> Cleanup: "Removes(reference)"
AddAutoMapper -> Fixer: "Fixed (removed) or Manual (IMapper is still used)"
```

## Figure 3: decision tree (per candidate, per round of the fixed point)

```d2
q1: "IMapper field / constructor parameter / local / mapper factory?"
q2: "Initializer null, default or droppable?"
q3: "Every use a migrated, not kept call, or inside a removal?"
q4: "Parameter: every caller passes a droppable value (or none)?"
q5: "Parameter: no other constructor gets the same signature?"
keep: "stays (no finding; AddAutoMapper Manual: IMapper is still used)"
go: "removed (Fixed, per type)"
q1 -> keep: no
q1 -> q2: yes
q2 -> keep: no
q2 -> q3: yes
q3 -> keep: no
q3 -> q4: "yes, parameter"
q3 -> go: "yes, other"
q4 -> keep: no
q4 -> q5: yes
q5 -> keep: no
q5 -> go: yes
```

| Question                         | Leaf                     | Proof / test                                                                 |
| -------------------------------- | ------------------------ | ---------------------------------------------------------------------------- |
| only assigned                    | removed                  | `assigned-only`                                                              |
| uses are migrated calls          | removed, registration too | `_Combined/unused-imapper-registration`                                     |
| caller passes a factory value    | removed with factory     | `test-factory`                                                               |
| null checks on the parameter     | removed                  | `null-checks`                                                                |
| DI lookup with `== null` check   | removed, with a warning  | `service-lookup`                                                             |
| handed out, `null` passed, clash | stays                    | `FixAsync_MapperUsedOtherwise_KeepsItAndTheRegistration`                     |
| lookup without AddAutoMapper     | stays                    | `FixAsync_ServiceLookupNotRegisteredByAddAutoMapperOrNotANullCheck_KeepsTheLookup` |

## See also

- [plan](automapper-unused-imapper-plan.md), [verify](automapper-unused-imapper-verify.md),
  `src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnusedMapperPattern.cs`, `src/Unshackle.Tests/Proofs/AutoMapper/UnusedMapper/`
