# Models: efbulk.unsupported

Facts from `src/Unshackle.Core/Fixes/EfBulk/Patterns/UnsupportedPattern.cs`, `EfBulkFix.Package` and `XmlProjectFileStore.EditAsync`.

## Figure 1: classes

**Figure 1.** The pattern, the fix's package migration and the engine path that applies it.

```mermaid
classDiagram
    class IPattern
    class UnsupportedPattern
    class EfBulkFix
    class PackageMigration
    class ProjectPackages
    class XmlProjectFileStore
    IPattern <|.. UnsupportedPattern
    EfBulkFix --> UnsupportedPattern
    EfBulkFix --> PackageMigration
    ProjectPackages ..> PackageMigration
    XmlProjectFileStore ..> PackageMigration
```

| Element | Role |
| --- | --- |
| `UnsupportedPattern` | Finds Z calls/constructors other than the batch extensions; always `Manual(Unsupported, reason)` |
| `EfBulkFix.Package` | Remove Plus, also remove Extensions, add nothing |
| `ProjectPackages` | Keeps the package for any bound Z symbol, also for referenced projects of a keeping project |
| `XmlProjectFileStore` | With no added package, removes `RemovedPackage` like an also-removed one |

## Figure 2: sequence

**Figure 2.** One run on a project with a `Future` call left.

```mermaid
sequenceDiagram
    Fixer->>UnsupportedPattern: Find / Rewrite
    UnsupportedPattern-->>Fixer: Manual(Unsupported: 'db.Orders.Future()' ...)
    Fixer->>ProjectPackages: PlanAsync
    ProjectPackages-->>Fixer: KeepRemovedPackage = true
    Fixer->>XmlProjectFileStore: EditAsync (package kept)
    Fixer-->>Fixer: project row "still used", rows with file:line
```

## Figure 3: decision tree

**Figure 3.** Reason per call.

```mermaid
flowchart TD
    A[Call binds to a Z assembly] -->|batch Delete/Update extension| X[efbulk.delete / efbulk.update]
    A -->|name has Bulk| B[Manual: no bulk in EF Core; EFCore.BulkExtensions.MIT alternative]
    A -->|Future / Deferred| C[Manual: no query batching]
    A -->|Cache| D[Manual: no second-level cache]
    A -->|Include*| E[Manual: filtered Include differs]
    A -->|Audit| F[Manual: no audit trail]
    A -->|other| G[Manual: no known twin]
```

## See also

- [efbulk-unsupported-plan.md](efbulk-unsupported-plan.md)
- [efbulk-unsupported-verify.md](efbulk-unsupported-verify.md)