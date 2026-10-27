# Models: xunit3.async-void-test

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/AsyncVoidTestPattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class AsyncVoidTestPattern
    class XunitV3Assemblies
    class XunitV3Reason
    IPattern <|.. AsyncVoidTestPattern
    AsyncVoidTestPattern --> XunitV3Assemblies
    AsyncVoidTestPattern --> XunitV3Reason
```

| Element | Role |
| --- | --- |
| `AsyncVoidTestPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `XunitV3Assemblies` | Names of the v2 assemblies; `IsFrom` checks a symbol's assembly |
| `XunitV3Reason` | Typed reason codes of the Manual rows |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>AsyncVoidTestPattern: Find(root, model)
    AsyncVoidTestPattern-->>Fixer: nodes
    Fixer->>AsyncVoidTestPattern: Rewrite(node, model)
    AsyncVoidTestPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[async void method] --> B{test attribute deriving from FactAttribute?}
    B -- no --> N[not found]
    B -- yes --> C{called or referenced in the file?}
    C -- yes --> M1[Manual AsyncVoidCalled]
    C -- no --> F[async Task]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `fact` |
| Manual `AsyncVoidCalled` | `called` |

## See also

- [xunit3-async-void-test-plan.md](xunit3-async-void-test-plan.md)
- [xunit3-async-void-test-verify.md](xunit3-async-void-test-verify.md)
