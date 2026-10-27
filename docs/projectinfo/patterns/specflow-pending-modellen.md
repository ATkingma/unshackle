# Models: specflow.pending

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/PendingPattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class PendingPattern
    class SpecFlowNames
    IPattern <|.. PendingPattern
    PendingPattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `PendingPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>PendingPattern: Find(root, model)
    PendingPattern-->>Fixer: nodes
    Fixer->>PendingPattern: Rewrite(node, model)
    PendingPattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[ScenarioContext.Pending or StepIsPending call] --> B{plain receiver, no arguments?}
    B -- no --> M1[Manual]
    B -- yes --> C{last statement or expression body?}
    C -- yes --> F[throw new PendingStepException]
    C -- no --> M2[Manual: not the last statement]
```

| Leaf | Proof |
| --- | --- |
| Fixed | skeleton |
| Manual | not-last |

## See also

- [specflow-pending-plan.md](specflow-pending-plan.md)
- [specflow-pending-verify.md](specflow-pending-verify.md)
