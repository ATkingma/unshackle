# Modellen: automapper.add-automapper

Feiten uit `src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs`.

## Figuur 1: klassendiagram

```d2
ISolutionPattern: {shape: class; "+Find(root, model)": "IEnumerable<SyntaxNode>"; "+RewriteAsync(node, model, solution, ct)": "Task<PatternResult>"}
AddAutoMapperPattern: {shape: class; "-s_services": "string[]"; "+Find()": ""; "+Rewrite()": "PatternResult"; "+RewriteAsync()": "Task<PatternResult>"; "-UnusedParameters()": "List<string>"; "-IsService()": "bool"}
PatternResult: {shape: class}
SymbolFinder: {shape: class}
AddAutoMapperPattern -> ISolutionPattern: implementeert
AddAutoMapperPattern -> SymbolFinder: FindReferencesAsync
AddAutoMapperPattern -> PatternResult: Fixed / Manual
```

| Element                | Rol                                                                  |
| ---------------------- | -------------------------------------------------------------------- |
| `AddAutoMapperPattern` | vindt `AddAutoMapper`, beslist weg of Manual                         |
| `s_services`           | `IMapper`, `IMapperBase`, `IRuntimeMapper`, `IConfigurationProvider` |
| `SymbolFinder`         | zoekt service-gebruik in de hele solution                            |
| `PatternResult`        | `Fixed([_ = p;])` of `Manual(reden)`                                 |

## Figuur 2: sequence

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

| Stap                | Uitleg                                                              |
| ------------------- | ------------------------------------------------------------------- |
| Find                | `AddAutoMapper` uit assembly `AutoMapper`; de statement als die er is |
| FindReferencesAsync | eerste verwijzing wordt `bestand:regel`                             |
| Rewrite             | beslisboom hieronder                                                |

## Figuur 3: beslisboom

```d2
q1: Losse statement?
q2: Mapper-service nog gebruikt?
m1: "Manual: The result of AddAutoMapper is used (result-used)"
m2: "Manual: IMapper is still used (mapper-injected)"
f: "Fixed: regel weg, _ = p; voor ongebruikte parameters (simple, parameter)"
q1 -> m1: nee
q1 -> q2: ja
q2 -> m2: ja
q2 -> f: nee
```

| Vraag              | Blad      | Proof                 |
| ------------------ | --------- | --------------------- |
| resultaat gebruikt | Manual    | `result-used`         |
| service gebruikt   | Manual    | `mapper-injected`     |
| anders             | regel weg | `simple`, `parameter` |

## Zie ook

- [plan](automapper-add-automapper-plan.md), [verify](automapper-add-automapper-verify.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs`, `src/Unshackle.Tests/Proofs/AutoMapper/AddAutoMapper/`
