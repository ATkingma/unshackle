# Models: efbulk.update

Facts from `src/Unshackle.Core/Fixes/EfBulk/Patterns/UpdatePattern.cs` and `src/Unshackle.Core/Fixes/EfBulk/EfBulkCall.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helper it uses.

```mermaid
classDiagram
    class IPattern
    class UpdatePattern
    class EfBulkCall
    class EfBulkFix
    IPattern <|.. UpdatePattern
    EfBulkFix --> UpdatePattern
    UpdatePattern --> EfBulkCall
```

| Element | Role |
| --- | --- |
| `UpdatePattern` | `Find` selects the Z calls, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `EfBulkCall` | Assembly-based match (`OldMethod`), EF version guard (`HasNative`), token check, rename |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>UpdatePattern: Find(root, model)
    UpdatePattern->>EfBulkCall: OldMethod(invocation, model, names)
    UpdatePattern-->>Fixer: invocations
    Fixer->>UpdatePattern: Rewrite(invocation, model)
    UpdatePattern->>EfBulkCall: HasNative(model, method)
    UpdatePattern-->>Fixer: Fixed (+ using Microsoft.EntityFrameworkCore) or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[Z batch update on IQueryable] --> B{ExecuteUpdate in the compilation?}
    B -- no --> M1[Manual NoNativeExecute]
    B -- yes --> C{factory, plus at most a CancellationToken?}
    C -- no --> M2[Manual BatchOptions]
    C -- yes --> D{inline x => new T with an object initializer?}
    D -- no --> M3[Manual UpdateFactoryShape]
    D -- yes --> E{every target scalar and every value plain?}
    E -- no --> M4[Manual UpdateValueNotProvable]
    E -- yes --> F[Fixed: ExecuteUpdate with a SetProperty chain]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `plus-initializer`, `extensions-async` |
| Manual `UpdateFactoryShape` | `factory-variable` |
| Manual `UpdateValueNotProvable` | `method-call`, `navigation` |

## See also

- [efbulk-update-plan.md](efbulk-update-plan.md)
- [efbulk-update-verify.md](efbulk-update-verify.md)
