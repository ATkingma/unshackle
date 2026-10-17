# Models: AutoMapper partial migration (TAAK-272)

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs`, `CreateMapPattern.cs`, `NestedMaps.cs`, `MapCallPattern.cs`,
`KeptMapPattern.cs`, `AutoMapperFix.cs` and `src/Unshackle.Core/Fixer.cs`.

## Figure 1: class diagram

```d2
IFix: {shape: class; "+AllOrNothing": "bool"; "+AllowsPartial(finding)": "bool"}
AutoMapperFix: {shape: class; "+AllowsPartial(finding)": "reason starts with KeptPrefix"}
Fixer: {shape: class; "+FixAsync()": "undoes all only for a Manual finding that is not allowed"}
MapOwners: {shape: class; "+KeptPrefix": "string"; "+ForAsync(solution, model, ct)": "Task<MapOwners>"; "+KeptCallAsync(call, model, solution, ct)": "Task<PatternResult?>"; "+Reason(source, destination)": "string?"; "-BuildAsync()": "fixpoint"; "-Close(nodes)": "closure"}
MapNode: {shape: class; "+Statement": "SyntaxNode"; "+Reaches(other)": "bool"}
CreateMapPattern: {shape: class; "+PlanAsync(profile, …)": "statement problems"; "-Rewrite(…, owners, problems)": "trimmed profile + Extensions"}
NestedMaps: {shape: class; "+FromAsync()": "without kept pairs"}
CallPatterns: "MapCallPattern / MapIntoPattern / ProjectToPattern"
KeptMapPattern: {shape: class}
AutoMapperFix -> IFix: implements
Fixer -> IFix: AllOrNothing, AllowsPartial
MapOwners -> MapNode: one per CreateMap statement
MapOwners -> CreateMapPattern: PlanAsync per profile
MapOwners -> CallPatterns: RewriteAsync per call
CreateMapPattern -> MapOwners: Reason (skip kept statements)
NestedMaps -> MapOwners: Reason (leave kept pairs out)
CallPatterns -> MapOwners: KeptCallAsync
KeptMapPattern -> MapOwners: Reason
```

| Element                 | Role                                                                                                   |
| ----------------------- | ------------------------------------------------------------------------------------------------------ |
| `MapOwners`             | plan: pairs (and destinations, or everything) that stay on AutoMapper, each with its reason             |
| `MapNode`               | a CreateMap statement with every type AutoMapper may meet while running it (over-approximated)          |
| `CreateMapPattern`      | rewrites the statements that are not kept; with `problems` it records a failing statement and goes on   |
| `NestedMaps`            | a kept pair has no `ToY()`, so a migrated member of that pair becomes a problem (and is kept)           |
| call patterns           | a call of a kept pair (or a mock set-up) stays: `Manual(Stays on AutoMapper: …)`                         |
| `IFix.AllowsPartial`    | the old all-or-nothing net still holds for every Manual finding that is not a planned keep             |

## Figure 2: sequence (building the plan)

```d2
shape: sequence_diagram
Pattern: any AutoMapper pattern
Owners: MapOwners
CMP: CreateMapPattern
Calls: call patterns
Pattern -> Owners: ForAsync(solution, model)
Owners -> Owners: "cached? (same solution apart from the file being fixed)"
Owners -> Owners: "BuildAsync: collect profiles, CreateMap statements, calls; s_planning = this"
Owners -> CMP: "PlanAsync(profile) (loop)"
CMP -> Owners: "statement problems → Keep"
Owners -> Calls: "RewriteAsync(call) (loop)"
Calls -> Owners: "Manual without KeptPrefix → KeepCall (pair, destination or all)"
Owners -> Owners: "Close: kept statement keeps its reverse and every reached map"
Owners -> Owners: "repeat until the plan stops growing"
Owners -> Pattern: plan
```

| Step      | Explanation                                                                                                |
| --------- | ---------------------------------------------------------------------------------------------------------- |
| cache     | the Fixer fixes each file against the original solution; a plan of that solution serves every file         |
| PlanAsync | the profile rewrite with the plan so far; a statement problem keeps that pair, a profile problem all pairs |
| calls     | the real call patterns decide; mock set-ups (Moq, NSubstitute, FakeItEasy) are kept without asking        |
| Close     | REQ-2; REQ-3 follows from `NestedMaps` leaving kept pairs out in the next round                            |

## Figure 3: decision tree (one CreateMap pair)

```d2
q1: "Statement or profile problem?"
q2: "A call of the pair not migrated (or a mock set-up)?"
q3: "Reached by a kept map (member, element, MapFrom, Include types)?"
q4: "Its ToB() would call the ToY() of a kept pair?"
k: "Stays on AutoMapper (CreateMap stays, calls stay on IMapper)"
m: "Migrated (ToB(), calls rewritten, CreateMap removed)"
q1 -> k: yes
q1 -> q2: no
q2 -> k: yes
q2 -> q3: no
q3 -> k: yes
q3 -> q4: no
q4 -> k: yes
q4 -> m: no
```

| Question            | Leaf | Evidence                                                       |
| ------------------- | ---- | -------------------------------------------------------------- |
| statement problem   | kept | `_Combined/partial-statement`, `MapOwnersTests`                 |
| call not migrated   | kept | `_Combined/partial-untyped-source`, mock test in `MapOwnersTests` |
| reached by kept map | kept | `_Combined/partial-closure` (REQ-2)                             |
| calls a kept map    | kept | `MapOwnersTests.FixAsync_KeptMap_KeepsTheMapsItIsConnectedWith` (REQ-3) |

## See also

- [plan](automapper-partial-migration-plan.md), [verify](automapper-partial-migration-verify.md), [automapper.kept-map](../patterns/automapper-kept-map-plan.md)
