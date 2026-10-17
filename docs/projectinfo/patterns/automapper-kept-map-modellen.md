# Models: automapper.kept-map

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/KeptMapPattern.cs` and `MapOwners.cs`.

## Figure 1: class diagram

```d2
ISolutionPattern: {shape: class; "+Find(root, model)": "IEnumerable<SyntaxNode>"; "+RewriteAsync(node, model, solution, ct)": "Task<PatternResult>"}
KeptMapPattern: {shape: class; "+Find()": ""; "+Rewrite()": "PatternResult"; "+RewriteAsync()": "Task<PatternResult>"; "-Kept()": "PatternResult"}
MapOwners: {shape: class; "+KeptPrefix": "string"; "+ForAsync(solution, model, ct)": "Task<MapOwners>"; "+Reason(source, destination)": "string?"; "+StatementPair(statement, model)": "(From, To, Reverse)?"}
KeptMapPattern -> ISolutionPattern: implements
KeptMapPattern -> MapOwners: plan and reason
```

| Element          | Role                                                             |
| ---------------- | ---------------------------------------------------------------- |
| `KeptMapPattern` | finds the statements left in a trimmed profile, reports them     |
| `MapOwners`      | the solution-wide plan: which pairs stay on AutoMapper, and why  |

## Figure 2: sequence

```d2
shape: sequence_diagram
Fixer
Pattern: KeptMapPattern
Owners: MapOwners
Fixer -> Pattern: Find(root, model)
Fixer -> Pattern: RewriteAsync(statement, model, solution, ct)
Pattern -> Owners: ForAsync(solution, model, ct)
Pattern -> Owners: Reason(from, to)
Pattern -> Fixer: "Manual(Stays on AutoMapper: …)"
```

## Figure 3: decision tree

```d2
q1: "Profile with its <Profile>Extensions class in the file?"
q2: "Statement with a CreateMap<A, B> pair?"
n: not found
m: "Manual: Stays on AutoMapper: CreateMap<A, B> stays in the profile … (trimmed-profile)"
q1 -> n: no
q1 -> q2: yes
q2 -> n: no
q2 -> m: yes
```

| Question                 | Leaf      | Proof             |
| ------------------------ | --------- | ----------------- |
| profile partly rewritten | Manual    | `trimmed-profile` |
| otherwise                | not found | —                 |

## See also

- [plan](automapper-kept-map-plan.md), [verify](automapper-kept-map-verify.md)
