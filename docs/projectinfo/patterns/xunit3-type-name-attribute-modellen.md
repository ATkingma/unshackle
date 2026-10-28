# Models: xunit3.type-name-attribute

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/TypeNameAttributePattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class TypeNameAttributePattern
    class XunitV3Assemblies
    class XunitV3Reason
    IPattern <|.. TypeNameAttributePattern
    TypeNameAttributePattern --> XunitV3Assemblies
    TypeNameAttributePattern --> XunitV3Reason
```

| Element | Role |
| --- | --- |
| `TypeNameAttributePattern` | `Find` selects the nodes, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `XunitV3Assemblies` | Names of the v2 assemblies; `IsFrom` checks a symbol's assembly |
| `XunitV3Reason` | Typed reason codes of the Manual rows |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>TypeNameAttributePattern: Find(root, model)
    TypeNameAttributePattern-->>Fixer: nodes
    Fixer->>TypeNameAttributePattern: Rewrite(node, model)
    TypeNameAttributePattern-->>Fixer: Fixed or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Find` and `Rewrite` ask.

```mermaid
flowchart TD
    A[attribute with 2 arguments] --> B{v2 CollectionBehavior/TestCaseOrderer/TestCollectionOrderer/TestFramework string ctor?}
    B -- no --> N[not found]
    B -- yes --> C{both constant strings?}
    C -- no --> M1[Manual TypeNameArgument]
    C -- yes --> D{type declared in this project and assembly name equal?}
    D -- no --> M1
    D -- yes --> F["typeof(Type)"]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `own-orderer` |
| Manual `TypeNameArgument` | `other-assembly` |

## See also

- [xunit3-type-name-attribute-plan.md](xunit3-type-name-attribute-plan.md)
- [xunit3-type-name-attribute-verify.md](xunit3-type-name-attribute-verify.md)
