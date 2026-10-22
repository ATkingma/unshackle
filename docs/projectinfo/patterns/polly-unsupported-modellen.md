# Models: polly.unsupported

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/UnsupportedPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyRetry.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class UnsupportedPattern
    class PollyRetry
    IPattern <|.. UnsupportedPattern
    UnsupportedPattern --> PollyRetry
```

| Element | Role |
| --- | --- |
| `UnsupportedPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `PollyRetry` | Shared holder, Execute and name analysis (`IsTimeout`, `IsNoOp`, `Targets`, `PlanDeclaration`, `PlanExecute`) |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>UnsupportedPattern: Find(root, model)
    UnsupportedPattern-->>Fixer: nodes
    Fixer->>UnsupportedPattern: Rewrite(node, model)
    UnsupportedPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[node] --> B{Polly call or creation?}
    B -- no --> N[not reported]
    B -- yes --> C{Fallback, Bulkhead, Cache, RateLimit, ExecuteAndCapture, PolicyRegistry, Context, Contrib}
    C -- one of them --> M[Manual with that code]
    C -- other --> N
```

| Leaf | Proof |
| --- | --- |
| Manual `<Kind>NotMapped` | `fallback`, `bulkhead`, `rate-limit`, `registry`, `context`, `execute-and-capture` |

## See also

- [polly-unsupported-plan.md](polly-unsupported-plan.md)
- [polly-unsupported-verify.md](polly-unsupported-verify.md)
- [polly-sync-retry-modellen.md](polly-sync-retry-modellen.md)
