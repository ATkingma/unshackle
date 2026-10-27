# Models: specflow.combined-tags

Facts from `src/Unshackle.Core/Fixes/SpecFlow/Patterns/CombinedTagsPattern.cs` and `src/Unshackle.Core/Fixes/SpecFlow/SpecFlowNames.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the name map it uses.

```mermaid
classDiagram
    class IPattern
    class CombinedTagsPattern
    class SpecFlowNames
    IPattern <|.. CombinedTagsPattern
    CombinedTagsPattern --> SpecFlowNames
```

| Element | Role |
| --- | --- |
| `CombinedTagsPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(reason)` |
| `SpecFlowNames` | Namespace swap, rename table and removed types of SpecFlow 3.9.74 → Reqnroll 3.3.4 |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>CombinedTagsPattern: Find(root, model)
    CombinedTagsPattern-->>Fixer: nodes
    Fixer->>CombinedTagsPattern: Rewrite(node, model)
    CombinedTagsPattern-->>Fixer: Fixed or Manual(reason)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[identifier ScenarioAndFeatureTags] --> B{binds to TechTalk.SpecFlow.ScenarioInfo?}
    B -- yes --> F[CombinedTags]
    B -- no --> N[not found]
```

| Leaf | Proof |
| --- | --- |
| Fixed | tags |

## See also

- [specflow-combined-tags-plan.md](specflow-combined-tags-plan.md)
- [specflow-combined-tags-verify.md](specflow-combined-tags-verify.md)
