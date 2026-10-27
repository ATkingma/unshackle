# Models: xunit3.async-lifetime

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/AsyncLifetimePattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class AsyncLifetimePattern
    class XunitV3Assemblies
    class XunitV3Reason
    IPattern <|.. AsyncLifetimePattern
    AsyncLifetimePattern --> XunitV3Assemblies
    AsyncLifetimePattern --> XunitV3Reason
```

| Element | Role |
| --- | --- |
| `AsyncLifetimePattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `XunitV3Assemblies` | Names of the v2 assemblies; `IsFrom` checks a symbol's assembly |
| `XunitV3Reason` | Typed reason codes of the Manual rows |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>AsyncLifetimePattern: Find(root, model)
    AsyncLifetimePattern-->>Fixer: nodes
    Fixer->>AsyncLifetimePattern: Rewrite(node, model)
    AsyncLifetimePattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[Task InitializeAsync/DisposeAsync implementing IAsyncLifetime] --> B{class also IDisposable/IAsyncDisposable?}
    B -- yes --> M1[Manual DualDisposal]
    B -- no --> C{used as a Task somewhere?}
    C -- yes, not only direct uses --> M2[Manual LifetimeResultUsed]
    C -- only direct uses --> D2[call sites get .AsTask, X6] --> D
    C -- no --> D{return type written as Task?}
    D -- no --> M3[Manual UnsupportedShape]
    D -- yes --> E{async?}
    E -- yes --> F[ValueTask return type]
    E -- no --> G[ValueTask + default / new ValueTask per return]
```

| Leaf | Proof |
| --- | --- |
| Fixed async | `async-methods`, `explicit` |
| Fixed non-async | `returned-task`, `explicit` |
| Manual `DualDisposal` | `disposable` |
| Manual `LifetimeResultUsed` | `result-used` |
| Fixed call site `.AsTask()` | `as-task` |

## See also

- [xunit3-async-lifetime-plan.md](xunit3-async-lifetime-plan.md)
- [xunit3-async-lifetime-verify.md](xunit3-async-lifetime-verify.md)
