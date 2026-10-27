# Models: specflow.renamed-type

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/RenamedTypePattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class RenamedTypePattern
    class SpecFlowNames
    IPattern <|.. RenamedTypePattern
    RenamedTypePattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `RenamedTypePattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>RenamedTypePattern: Find(root, model)
    RenamedTypePattern-->>Fixer: nodes
    Fixer->>RenamedTypePattern: Rewrite(node, model)
    RenamedTypePattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[simple name binding to a SpecFlow type] --> B{in the rename table?}
    B -- yes --> F[new simple name + its namespace]
    B -- no --> C{in the removed list?}
    C -- yes --> M[Manual]
    C -- no --> N[not found: the namespace swap is enough]
```

| Leaf | Proof |
| --- | --- |
| Fixed rename | output-helper, table-row |
| Manual | removed-type |

## See also

- [specflow-renamed-type-plan.md](specflow-renamed-type-plan.md)
- [specflow-renamed-type-verify.md](specflow-renamed-type-verify.md)
