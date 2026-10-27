# Models: specflow.async-void-binding

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/AsyncVoidBindingPattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class AsyncVoidBindingPattern
    class SpecFlowNames
    IPattern <|.. AsyncVoidBindingPattern
    AsyncVoidBindingPattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `AsyncVoidBindingPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>AsyncVoidBindingPattern: Find(root, model)
    AsyncVoidBindingPattern-->>Fixer: nodes
    Fixer->>AsyncVoidBindingPattern: Rewrite(node, model)
    AsyncVoidBindingPattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[async void method] --> B{has a SpecFlow step or hook attribute?}
    B -- yes --> M[Manual]
    B -- no --> N[not found]
```

| Leaf | Proof |
| --- | --- |
| Manual | async-void |

## See also

- [specflow-async-void-binding-plan.md](specflow-async-void-binding-plan.md)
- [specflow-async-void-binding-verify.md](specflow-async-void-binding-verify.md)
