# Models: automapper.add-automapper

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs`.

## Figure 1: class diagram

```d2
ISolutionPattern: {shape: class; "+Find(root, model)": "IEnumerable<SyntaxNode>"; "+RewriteAsync(node, model, solution, ct)": "Task<PatternResult>"}
AddAutoMapperPattern: {shape: class; "-s_services": "string[]"; "+Find()": ""; "+Rewrite()": "PatternResult"; "+RewriteAsync()": "Task<PatternResult>"; "-UnusedParameters()": "List<string>"; "-IsService()": "bool"}
PatternResult: {shape: class}
SymbolFinder: {shape: class}
AddAutoMapperPattern -> ISolutionPattern: implements
AddAutoMapperPattern -> SymbolFinder: FindReferencesAsync
AddAutoMapperPattern -> PatternResult: Fixed / Manual
```

| Element                | Role                                                                 |
| ---------------------- | -------------------------------------------------------------------- |
| `AddAutoMapperPattern` | finds `AddAutoMapper`, decides removed or Manual                     |
| `s_services`           | `IMapper`, `IMapperBase`, `IRuntimeMapper`, `IConfigurationProvider` |
| `SymbolFinder`         | looks for service usage in the whole solution                        |
| `PatternResult`        | `Fixed([_ = p;])` or `Manual(reason)`                                |

## Figure 2: sequence

```d2
shape: sequence_diagram
Fixer
Pattern: AddAutoMapperPattern
SF: SymbolFinder
Fixer -> Pattern: Find(root, model)
Fixer -> Pattern: RewriteAsync(node, model, solution, ct)
Pattern -> SF: FindReferencesAsync(service) per s_services
Pattern -> Pattern: Rewrite(node, model, user)
Pattern -> Fixer: Fixed / Manual
```

| Step                | Explanation                                                         |
| ------------------- | ------------------------------------------------------------------- |
| Find                | `AddAutoMapper` from assembly `AutoMapper`; the statement if there is one |
| FindReferencesAsync | the first reference becomes `file:line`                             |
| Rewrite             | decision tree below                                                 |

## Figure 3: decision tree

```d2
q1: Standalone statement?
q2: Mapper service still used?
m1: "Manual: The result of AddAutoMapper is used (result-used)"
m2: "Manual: IMapper is still used (mapper-injected)"
f: "Fixed: line removed, _ = p; for unused parameters (simple, parameter)"
q1 -> m1: no
q1 -> q2: yes
q2 -> m2: yes
q2 -> f: no
```

| Question           | Leaf         | Proof                 |
| ------------------ | ------------ | --------------------- |
| result used        | Manual       | `result-used`         |
| service used       | Manual       | `mapper-injected`     |
| otherwise          | line removed | `simple`, `parameter` |

## See also

- [plan](automapper-add-automapper-plan.md), [verify](automapper-add-automapper-verify.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs`, `src/Unshackle.Tests/Proofs/AutoMapper/AddAutoMapper/`
