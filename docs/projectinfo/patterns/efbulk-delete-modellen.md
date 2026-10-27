# Models: efbulk.delete

Facts from `src/Unshackle.Core/Fixes/EfBulk/Patterns/DeletePattern.cs` and `src/Unshackle.Core/Fixes/EfBulk/EfBulkCall.cs`.

## Figure 1: classes

**Figure 1.** The pattern and the helper it uses.

```mermaid
classDiagram
    class IPattern
    class DeletePattern
    class EfBulkCall
    class EfBulkFix
    IPattern <|.. DeletePattern
    EfBulkFix --> DeletePattern
    DeletePattern --> EfBulkCall
```

| Element | Role |
| --- | --- |
| `DeletePattern` | `Find` selects the Z calls, `Rewrite` returns `Fixed` or `Manual(code, reason)` |
| `EfBulkCall` | Assembly-based match (`OldMethod`), EF version guard (`HasNative`), token check, rename |

## Figure 2: sequence

**Figure 2.** The flow the Fixer drives.

```mermaid
sequenceDiagram
    Fixer->>DeletePattern: Find(root, model)
    DeletePattern->>EfBulkCall: OldMethod(invocation, model, names)
    DeletePattern-->>Fixer: invocations
    Fixer->>DeletePattern: Rewrite(invocation, model)
    DeletePattern->>EfBulkCall: HasNative(model, method)
    DeletePattern-->>Fixer: Fixed (+ using Microsoft.EntityFrameworkCore) or Manual(code: text)
```

## Figure 3: decision tree

**Figure 3.** What `Rewrite` asks.

```mermaid
flowchart TD
    A[Z batch delete on IQueryable] --> B{ExecuteDelete in the compilation?}
    B -- no --> M1[Manual NoNativeExecute]
    B -- yes --> C{no arguments, or async with only a CancellationToken?}
    C -- no --> M2[Manual BatchOptions]
    C -- yes --> F[Fixed: ExecuteDelete / ExecuteDeleteAsync]
```

| Leaf | Proof |
| --- | --- |
| Fixed | `plus-sync`, `plus-async-token`, `extensions-from-query` |
| Manual `BatchOptions` | `batch-builder` |
| Manual `NoNativeExecute` | unit test |

## See also

- [efbulk-delete-plan.md](efbulk-delete-plan.md)
- [efbulk-delete-verify.md](efbulk-delete-verify.md)
