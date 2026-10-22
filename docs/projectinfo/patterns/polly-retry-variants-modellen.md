# Models: polly.retry-variants

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/RetryVariantsPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyRetry.cs`.

## Figure 1: classes

**Figure 1.** Two patterns share one analysis; `IsVariant` splits the retry calls between them.

```mermaid
classDiagram
    class WaitAndRetryAsyncPattern
    class RetryVariantsPattern
    class PollyRetry { IsRetryCreation() IsVariant() PlanPipeline() }
    class PollyPredicate
    class PollyOnRetry
    WaitAndRetryAsyncPattern --> PollyRetry
    RetryVariantsPattern --> PollyRetry
    PollyRetry --> PollyPredicate
    PollyRetry --> PollyOnRetry
```

| Element | Role |
| --- | --- |
| `RetryVariantsPattern` | Finds retry calls with `IsVariant` true |
| `PollyRetry.IsVariant` | not `WaitAndRetryAsync`, or `WaitAndRetryAsync` with a `sleepDurations` parameter |
| `PollyRetry.PlanPipeline` | `MaxRetryAttempts` and delay lines per method; filter and onRetry shared |

## Figure 2: sequence

**Figure 2.** Same as `polly.wait-and-retry-async` (Find → Targets; Rewrite → PlanDeclaration / PlanExecute → PlanPipeline);
only the `MaxRetryAttempts`/delay step differs.

```mermaid
sequenceDiagram
    Fixer->>RetryVariantsPattern: Find
    RetryVariantsPattern->>PollyRetry: IsRetryCreation && IsVariant, Targets
    Fixer->>RetryVariantsPattern: Rewrite(node)
    RetryVariantsPattern->>PollyRetry: PlanDeclaration / PlanExecute
    PollyRetry->>PollyRetry: PlanPipeline: count + delay by method
    PollyRetry-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** The method-specific part of `PlanPipeline`.

```mermaid
flowchart TD
    A[retry method] --> B{sleepDurations?}
    B -- yes --> C{literal TimeSpan list?}
    C -- no --> M1[Manual SleepDurationsNotLiteral]
    C -- yes --> F1[MaxRetryAttempts = count, DelayGenerator switch]
    B -- no --> D{Forever?}
    D -- yes --> G[int.MaxValue]
    D -- no --> E{retryCount given?}
    E -- no --> H[1]
    E -- yes --> I{literal >= 1?}
    I -- no --> M2[Manual RetryCountNotLiteral]
    I -- yes --> J[n]
    G --> K{provider?}
    H --> K
    J --> K
    K -- yes --> L[DelayGenerator with AttemptNumber + 1]
    K -- no --> N[Delay = TimeSpan.Zero]
```

| Leaf | Proof |
| --- | --- |
| list | `durations`, `durations-variable` |
| Forever | `wait-forever` |
| count / default | `retry-count`, `retry-default` |

## See also

- [polly-retry-variants-plan.md](polly-retry-variants-plan.md)
- [polly-retry-variants-verify.md](polly-retry-variants-verify.md)
- [polly-wait-and-retry-async-modellen.md](polly-wait-and-retry-async-modellen.md)
