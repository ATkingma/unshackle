# Models: polly.timeout

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/TimeoutPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyRetry.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class TimeoutPattern
    class PollyRetry
    IPattern <|.. TimeoutPattern
    TimeoutPattern --> PollyRetry
```

| Element | Role |
| --- | --- |
| `TimeoutPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `PollyRetry` | Shared holder, Execute and name analysis (`IsTimeout`, `IsNoOp`, `Targets`, `PlanDeclaration`, `PlanExecute`) |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>TimeoutPattern: Find(root, model)
    TimeoutPattern-->>Fixer: nodes
    Fixer->>TimeoutPattern: Rewrite(node, model)
    TimeoutPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[Policy.Timeout call] --> B{1 or 2 arguments?}
    B -- no --> M1[Manual OverloadNotSupported]
    B -- yes --> C{2nd argument}
    C -- other than TimeoutStrategy --> M1
    C -- Pessimistic --> M2[Manual TimeoutStrategyNotOptimistic]
    C -- none or Optimistic --> D{literal 100 ms .. 1 day?}
    D -- no --> M3[Manual TimeoutNotProvable]
    D -- yes --> E{holder and Execute calls rewritable?}
    E -- no --> M4[Manual from the shared holder rules]
    E -- yes --> F[AddTimeout pipeline]
```

| Leaf | Proof |
| --- | --- |
| Fixed AddTimeout | `token-honoured`, `token-ignored`, `int-seconds`, `sync`, `typed` |
| Manual `TimeoutStrategyNotOptimistic` | `pessimistic` |
| Manual `TimeoutNotProvable` | `too-small`, `variable` |
| Manual `OverloadNotSupported` | `on-timeout` |
| Manual `ExecuteNotLambda` | `token-sync-lambda` |

## See also

- [polly-timeout-plan.md](polly-timeout-plan.md)
- [polly-timeout-verify.md](polly-timeout-verify.md)
- [polly-sync-retry-modellen.md](polly-sync-retry-modellen.md)
