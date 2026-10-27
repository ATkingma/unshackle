# Models: polly.add-policy-handler

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/AddPolicyHandlerPattern.cs`, `PollyPredicate.cs` and `PollyRetry.PlanOptions`.

## Figure 1: classes

**Figure 1.** The handler pattern reuses the retry options of the other Polly patterns.

```mermaid
classDiagram
    class AddPolicyHandlerPattern { Find() Rewrite() OtherHandler() }
    class PollyRetry { PlanOptions() IsRetryCreation() }
    class PollyPredicate { Plan() Transient() }
    AddPolicyHandlerPattern --> PollyRetry
    PollyRetry --> PollyPredicate
```

| Element | Role |
| --- | --- |
| `AddPolicyHandlerPattern` | Finds `AddPolicyHandler`/`AddTransientHttpErrorPolicy` of Microsoft.Extensions.Http.Polly |
| `PollyRetry.PlanOptions` | `new RetryStrategyOptions<HttpResponseMessage> { ... }` text, shared with the pipeline patterns |
| `PollyPredicate` | v7 transient filter written out; `p` of the lambda counts as `HandleTransientHttpError()` |

## Figure 2: sequence

**Figure 2.** One node per handler call.

```mermaid
sequenceDiagram
    Fixer->>AddPolicyHandlerPattern: Find
    Fixer->>AddPolicyHandlerPattern: Rewrite(.AddPolicyHandler(...))
    AddPolicyHandlerPattern->>AddPolicyHandlerPattern: overload, other handlers, inline retry
    AddPolicyHandlerPattern->>PollyRetry: PlanOptions(creation)
    PollyRetry-->>AddPolicyHandlerPattern: options text or Manual
    AddPolicyHandlerPattern-->>Fixer: .AddResilienceHandler("retry", builder => builder.AddRetry(options))
```

## Figure 3: decision tree

**Figure 3.** Questions asked by `Rewrite`.

```mermaid
flowchart TD
    A[AddPolicyHandler / AddTransientHttpErrorPolicy] --> B{policy or p => overload?}
    B -- no --> M1[Manual HandlerOverloadNotSupported]
    B -- yes --> C{another handler on the chain?}
    C -- yes --> M2[Manual HandlerOrderNotProvable]
    C -- no --> U{builder result used?}
    U -- yes --> M5[Manual HandlerResultUsed]
    U -- no --> D{retry call written in place?}
    D -- no --> M3[Manual HandlerPolicyNotInline]
    D -- yes --> E{retry options plannable?}
    E -- no --> M4[Manual with the retry code]
    E -- yes --> F[Fixed AddResilienceHandler]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `inline-transient`, `transient-lambda-429` (+ traces) |
| `HandlerOrderNotProvable` | `two-handlers` |
| `HandlerResultUsed` | `result-returned`, unit tests |
| `HandlerPolicyNotInline` | unit tests |
| method inlined and removed | `policy-method` |
| `HandlerOverloadNotSupported`, retry codes | `AddPolicyHandlerPatternTests` |

## See also

- [polly-add-policy-handler-plan.md](polly-add-policy-handler-plan.md)
- [polly-add-policy-handler-verify.md](polly-add-policy-handler-verify.md)
