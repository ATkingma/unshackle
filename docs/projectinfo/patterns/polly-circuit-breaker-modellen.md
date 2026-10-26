# Models: polly.circuit-breaker

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/CircuitBreakerPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyRetry.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class CircuitBreakerPattern
    class PollyRetry
    IPattern <|.. CircuitBreakerPattern
    CircuitBreakerPattern --> PollyRetry
```

| Element | Role |
| --- | --- |
| `CircuitBreakerPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `PollyRetry` | Shared holder, Execute and name analysis (`IsTimeout`, `IsNoOp`, `Targets`, `PlanDeclaration`, `PlanExecute`) |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>CircuitBreakerPattern: Find(root, model)
    CircuitBreakerPattern-->>Fixer: nodes
    Fixer->>CircuitBreakerPattern: Rewrite(node, model)
    CircuitBreakerPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[CircuitBreaker call] --> B[Manual CircuitBreakerNotEquivalent]
```

| Leaf | Proof |
| --- | --- |
| Manual `CircuitBreakerNotEquivalent` | `consecutive`, `advanced` |

## See also

- [polly-circuit-breaker-plan.md](polly-circuit-breaker-plan.md)
- [polly-circuit-breaker-verify.md](polly-circuit-breaker-verify.md)
- [polly-sync-retry-modellen.md](polly-sync-retry-modellen.md)
