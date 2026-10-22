# Models: polly.wait-and-retry-async

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/WaitAndRetryAsyncPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyRetry.cs`.

## Figure 1: classes

**Figure 1.** The pattern, its analysis helper and the v7/v8 types.

```mermaid
classDiagram
    class WaitAndRetryAsyncPattern { Find(root, model) Rewrite(node, model) }
    class PollyRetry { Targets() PlanDeclaration() PlanExecute() PlanPipeline() }
    class PollyPlan { Node Reason }
    class PollyReason { <<enum>> }
    WaitAndRetryAsyncPattern --> PollyRetry
    PollyRetry --> PollyPlan
    PollyPlan --> PollyReason
```

| Element | Role |
| --- | --- |
| `WaitAndRetryAsyncPattern` | Finds `WaitAndRetryAsync` from assembly `Polly`; maps a `PollyPlan` to `Fixed`/`Manual` |
| `PollyRetry` | Shape analysis from the original tree; declaration and calls of one local share every check |
| `PollyReason` | Typed reason code that leads every Manual text |
| Polly v7 | `AsyncRetryPolicy`, `ExecuteAsync(Func<Task>)` → `Task` |
| Polly.Core 8 | `ResiliencePipeline`, `ExecuteAsync(Func<CancellationToken, ValueTask>)` → `ValueTask` |

## Figure 2: sequence

**Figure 2.** From the Fixer via `Find` and `Rewrite`.

```mermaid
sequenceDiagram
    Fixer->>Pattern: Find(root, model)
    Pattern->>PollyRetry: Targets(creation)
    PollyRetry-->>Fixer: local statement + ExecuteAsync calls, or inline call, or the creation
    Fixer->>Pattern: Rewrite(node)
    Pattern->>PollyRetry: PlanDeclaration / PlanExecute
    PollyRetry->>PollyRetry: PlanLocalUses, PlanPipeline, PlanCallback
    PollyRetry-->>Fixer: Fixed(node, System, System.Threading.Tasks, Polly, Polly.Retry) or Manual(code: text)
```

| Step | Method | What happens |
| --- | --- | --- |
| Find | `IsWaitAndRetryAsync`, `Targets` | outer nodes only, so replacements never nest |
| Uses | `PlanLocalUses` | every reference of the local must be an awaited rewritable `ExecuteAsync` |
| Pipeline | `PlanPipeline` | builder text at the statement's indentation; delay body re-indented into `SleepDuration` |
| Callback | `PlanCallback` | `async _ =>` or `_ => new ValueTask(<T>)(expr)` |

## Figure 3: decision tree

**Figure 3.** Questions asked by `Rewrite`.

```mermaid
flowchart TD
    A[WaitAndRetryAsync from Polly] --> B{inline .ExecuteAsync or local?}
    B -- neither --> M1[Manual PolicyNotLocal]
    B -- yes --> C{overload int, Func int TimeSpan?}
    C -- no --> M2[Manual OverloadNotSupported]
    C -- yes --> D{receiver Policy.Handle E without predicate?}
    D -- no --> M3[Manual ExceptionFilterNotSupported]
    D -- yes --> E{retry count literal >= 1?}
    E -- no --> M4[Manual RetryCountNotLiteral]
    E -- yes --> F{delay a one-parameter lambda, no args/SleepDuration names?}
    F -- no --> M5[Manual DelayNotLambda / NameClash]
    F -- yes --> G{every use an awaited ExecuteAsync with a parameterless lambda?}
    G -- no --> M6[Manual PolicyUsedBeyondExecute / ResultNotAwaited / ExecuteOverloadNotSupported / ExecuteNotLambda]
    G -- yes --> X[Fixed: ResiliencePipelineBuilder + AddRetry]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `local-block-delay`, `inline-result`, `typed-local-not-handled` (+ behaviour traces) |
| `PolicyNotLocal` | `policy-returned` |
| `OverloadNotSupported` | unit test (Context-taking provider) |
| onRetry Fixed / `OnRetryUsesContext` | `on-retry-attempt`, `on-retry-async`, `on-retry-result` / `on-retry-context` |
| `RetryCountNotLiteral` | `retry-count-variable` |
| `ResultNotAwaited` | `result-not-awaited` |
| other codes | `WaitAndRetryAsyncPatternTests.Rewrite_UnsupportedShape_EveryFindingManualWithCode` |

## See also

- [polly-wait-and-retry-async-plan.md](polly-wait-and-retry-async-plan.md)
- [polly-wait-and-retry-async-verify.md](polly-wait-and-retry-async-verify.md)
