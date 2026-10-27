# Models: specflow.string-step-call

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/StringStepCallPattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class StringStepCallPattern
    class SpecFlowNames
    IPattern <|.. StringStepCallPattern
    StringStepCallPattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `StringStepCallPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>StringStepCallPattern: Find(root, model)
    StringStepCallPattern-->>Fixer: nodes
    Fixer->>StringStepCallPattern: Rewrite(node, model)
    StringStepCallPattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[call Given/When/Then/And/But] --> B{binds to TechTalk.SpecFlow.Steps?}
    B -- yes --> M[Manual]
    B -- no --> N[not found]
```

| Leaf | Proof |
| --- | --- |
| Manual | steps-base |

## See also

- [specflow-string-step-call-plan.md](specflow-string-step-call-plan.md)
- [specflow-string-step-call-verify.md](specflow-string-step-call-verify.md)
