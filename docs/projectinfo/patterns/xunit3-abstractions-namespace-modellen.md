# Models: xunit3.abstractions-namespace

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/AbstractionsNamespacePattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class AbstractionsNamespacePattern
    class XunitV3Assemblies
    class XunitV3Reason
    IPattern <|.. AbstractionsNamespacePattern
    AbstractionsNamespacePattern --> XunitV3Assemblies
    AbstractionsNamespacePattern --> XunitV3Reason
```

| Element | Role |
| --- | --- |
| `AbstractionsNamespacePattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `XunitV3Assemblies` | Names of the v2 assemblies; `IsFrom` checks a symbol's assembly |
| `XunitV3Reason` | Typed reason codes of the Manual rows |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>AbstractionsNamespacePattern: Find(root, model)
    AbstractionsNamespacePattern-->>Fixer: nodes
    Fixer->>AbstractionsNamespacePattern: Rewrite(node, model)
    AbstractionsNamespacePattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[using Xunit.Abstractions or qualified name] --> B{other Abstractions type in the served files?}
    B -- yes --> M1[Manual ExtensibilityType]
    B -- no --> C{own ITestOutputHelper implementation?}
    C -- yes --> M2[Manual OutputHelperImplemented]
    C -- no --> D{using directive and scope has using Xunit?}
    D -- yes --> E[drop the directive]
    D -- no --> F[Xunit.Abstractions → Xunit]
```

| Leaf | Proof |
| --- | --- |
| Fixed swap | `swap-only`, `qualified` |
| Fixed drop | `output-helper` |
| Manual `ExtensibilityType` | `message-sink` |
| Manual `OutputHelperImplemented` | `own-helper` |

## See also

- [xunit3-abstractions-namespace-plan.md](xunit3-abstractions-namespace-plan.md)
- [xunit3-abstractions-namespace-verify.md](xunit3-abstractions-namespace-verify.md)
