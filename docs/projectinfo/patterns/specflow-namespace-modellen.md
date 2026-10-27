# Models: specflow.namespace

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/NamespacePattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class NamespacePattern
    class SpecFlowNames
    IPattern <|.. NamespacePattern
    NamespacePattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `NamespacePattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>NamespacePattern: Find(root, model)
    NamespacePattern-->>Fixer: nodes
    Fixer->>NamespacePattern: Rewrite(node, model)
    NamespacePattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[using or name binding to a SpecFlow/BoDi namespace or type] --> B{removed namespace or type?}
    B -- yes --> M[Manual: no Reqnroll counterpart]
    B -- no --> C{renamed type written in full?}
    C -- yes --> R[new full name from the rename table]
    C -- no --> S[namespace swap TechTalk.SpecFlow to Reqnroll, BoDi to Reqnroll.BoDi]
```

| Leaf | Proof |
| --- | --- |
| Fixed swap | steps, assist, bodi, qualified |
| Manual | app-config |

## See also

- [specflow-namespace-plan.md](specflow-namespace-plan.md)
- [specflow-namespace-verify.md](specflow-namespace-verify.md)
