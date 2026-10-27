# Models: xunit3.extensibility

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/ExtensibilityPattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class ExtensibilityPattern
    class XunitV3Assemblies
    class XunitV3Reason
    IPattern <|.. ExtensibilityPattern
    ExtensibilityPattern --> XunitV3Assemblies
    ExtensibilityPattern --> XunitV3Reason
```

| Element | Role |
| --- | --- |
| `ExtensibilityPattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `XunitV3Assemblies` | Names of the v2 assemblies; `IsFrom` checks a symbol's assembly |
| `XunitV3Reason` | Typed reason codes of the Manual rows |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>ExtensibilityPattern: Find(root, model)
    ExtensibilityPattern-->>Fixer: nodes
    Fixer->>ExtensibilityPattern: Rewrite(node, model)
    ExtensibilityPattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Find` and `Rewrite` ask.

```mermaid
flowchart TD
    A[class declaration] --> B{base type, own interface or attribute from v2 Xunit.Sdk, or ITestCollectionOrderer / MemberDataAttributeBase?}
    B -- no --> N[not found]
    B -- yes --> M1[Manual ExtensibilityType]
```

| Leaf | Proof |
| --- | --- |
| Manual `ExtensibilityType` | `custom-types` |

## See also

- [xunit3-extensibility-plan.md](xunit3-extensibility-plan.md)
- [xunit3-extensibility-verify.md](xunit3-extensibility-verify.md)
