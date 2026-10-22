# Models: polly.no-op

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/NoOpPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyRetry.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class NoOpPattern
    class PollyRetry
    IPattern <|.. NoOpPattern
    NoOpPattern --> PollyRetry
```

| Element | Role |
| --- | --- |
| `NoOpPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `PollyRetry` | Shared holder, Execute and name analysis (`IsTimeout`, `IsNoOp`, `Targets`, `PlanDeclaration`, `PlanExecute`) |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>NoOpPattern: Find(root, model)
    NoOpPattern-->>Fixer: nodes
    Fixer->>NoOpPattern: Rewrite(node, model)
    NoOpPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[Policy.NoOp call] --> B{held in a local, private field or inline?}
    B -- no --> M1[Manual PolicyNotLocal]
    B -- yes --> C{Execute calls rewritable?}
    C -- no --> M2[Manual from the shared holder rules]
    C -- yes --> D{generic?}
    D -- yes --> E[ResiliencePipeline of T .Empty]
    D -- no --> F[ResiliencePipeline.Empty]
```

| Leaf | Proof |
| --- | --- |
| Fixed Empty | `async`, `typed`, `sync-inline` |
| Manual `PolicyNotLocal` | `returned` |

## See also

- [polly-no-op-plan.md](polly-no-op-plan.md)
- [polly-no-op-verify.md](polly-no-op-verify.md)
- [polly-sync-retry-modellen.md](polly-sync-retry-modellen.md)
