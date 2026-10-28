# Models: xunit3.record-exception-async

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/RecordExceptionAsyncPattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class RecordExceptionAsyncPattern
    class XunitV3Assemblies
    class XunitV3Reason
    IPattern <|.. RecordExceptionAsyncPattern
    RecordExceptionAsyncPattern --> XunitV3Assemblies
    RecordExceptionAsyncPattern --> XunitV3Reason
    class XunitV3Syntax
    RecordExceptionAsyncPattern --> XunitV3Syntax
```

| Element | Role |
| --- | --- |
| `RecordExceptionAsyncPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `XunitV3Assemblies` | Names of the v2 assemblies; `IsFrom` checks a symbol's assembly |
| `XunitV3Reason` | Typed reason codes of the Manual rows |
| `XunitV3Syntax` | `IsAwaited`, `InvocationOf`: shared syntax checks |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>RecordExceptionAsyncPattern: Find(root, model)
    RecordExceptionAsyncPattern-->>Fixer: nodes
    Fixer->>RecordExceptionAsyncPattern: Rewrite(node, model)
    RecordExceptionAsyncPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Find` and `Rewrite` ask.

```mermaid
flowchart TD
    A[name ExceptionAsync] --> B{binds to v2 Xunit.Record?}
    B -- no --> N[not found]
    B -- yes --> C{directly awaited?}
    C -- yes --> N
    C -- no --> D{invocation?}
    D -- no, method group --> M1[Manual UnsupportedShape]
    D -- yes --> F[append .AsTask]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `as-task` |
| Manual `UnsupportedShape` | `method-group` |

## See also

- [xunit3-record-exception-async-plan.md](xunit3-record-exception-async-plan.md)
- [xunit3-record-exception-async-verify.md](xunit3-record-exception-async-verify.md)
