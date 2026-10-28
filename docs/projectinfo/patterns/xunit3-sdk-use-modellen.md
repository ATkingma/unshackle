# Models: xunit3.sdk-use

Facts from `src/Unshackle.Core/Fixes/XunitV3/Patterns/SdkUsePattern.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helpers it uses.

```mermaid
classDiagram
    class IPattern
    class SdkUsePattern
    class XunitV3Assemblies
    class XunitV3SdkTypes
    IPattern <|.. SdkUsePattern
    SdkUsePattern --> XunitV3Assemblies
    SdkUsePattern --> XunitV3SdkTypes
```

| Element | Role |
| --- | --- |
| `SdkUsePattern` | `Find` returns the root when the file uses a v2 `Xunit.Sdk` type; `Rewrite` returns one Info note |
| `XunitV3Assemblies` | Names of the v2 assemblies |
| `XunitV3SdkTypes` | `MissingInV3`: v2 `Xunit.Sdk` types without a same-name v3 type (checked by a test against the real assemblies) |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>SdkUsePattern: Find(root, model)
    SdkUsePattern-->>Fixer: [root] or none
    Fixer->>SdkUsePattern: Rewrite(root, model)
    SdkUsePattern-->>Fixer: Note(types, lines, missing in v3)
```

## Figure 3: decision tree

**Figure 3.** What `Find` and `Rewrite` ask per name.

```mermaid
flowchart TD
    A[simple name] --> B{in a using, or a declaration's base list / attribute?}
    B -- yes --> N[skip]
    B -- no --> C{binds to a v2 Xunit.Sdk type or its constructor?}
    C -- no --> N
    C -- yes --> D{in MissingInV3?}
    D -- yes --> I1[Info: type, lines, no same-name type in xunit.v3 3.2.2]
    D -- no --> I2[Info: type, lines]
```

| Leaf | Proof |
| --- | --- |
| Info with "no same-name type" | `changed-members` (`RunSummary`) |
| Info without | `changed-members` (`TestTimeoutException`) |

## See also

- [xunit3-sdk-use-plan.md](xunit3-sdk-use-plan.md)
- [xunit3-sdk-use-verify.md](xunit3-sdk-use-verify.md)
