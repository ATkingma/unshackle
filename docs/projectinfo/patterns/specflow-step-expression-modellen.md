# Models: specflow.step-expression

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/StepExpressionPattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class StepExpressionPattern
    class SpecFlowNames
    IPattern <|.. StepExpressionPattern
    StepExpressionPattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `StepExpressionPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>StepExpressionPattern: Find(root, model)
    StepExpressionPattern-->>Fixer: nodes
    Fixer->>StepExpressionPattern: Rewrite(node, model)
    StepExpressionPattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[attribute with a constant first string] --> B{SpecFlow step attribute?}
    B -- no --> N[not found]
    B -- yes --> C{Reqnroll detector: Cucumber expression?}
    C -- no --> N
    C -- yes --> D{holds a special character?}
    D -- no --> N
    D -- yes --> M[Manual]
```

| Leaf | Proof |
| --- | --- |
| Manual | optional-text |
| not found | `StepExpression_ChangesMeaning_*` (plain, regex-detected and anchored texts) |

## See also

- [specflow-step-expression-plan.md](specflow-step-expression-plan.md)
- [specflow-step-expression-verify.md](specflow-step-expression-verify.md)
