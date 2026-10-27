# Models: epplus.flow-types

Facts from `src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs` and `src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class FlowTypesPattern { Find(root, model); Rewrite(node, model); Migratable(method, model); MigratableField(field, model); MigratableReturn(method, model) }
    class WorkbookUses { WorkbookUses(model, scope, IParameterSymbol, chain); Map(); Replacements }
    class UnsupportedPattern
    UnsupportedPattern ..> FlowTypesPattern : Find (E24, skip Manual nodes)
    class PackagePattern
    IPattern <|.. FlowTypesPattern
    FlowTypesPattern ..> WorkbookUses
    PackagePattern ..> WorkbookUses
    WorkbookUses ..> FlowTypesPattern : IsFlowArgument / field write / return argument
    EPPlusFix o-- FlowTypesPattern
```

## Figure 2: sequence (full fix)

```mermaid
sequenceDiagram
    Fixer->>PackagePattern: Rewrite package scope
    PackagePattern->>WorkbookUses: Map (worksheet local passed to Fill)
    WorkbookUses->>FlowTypesPattern: Migratable(Fill)
    FlowTypesPattern-->>WorkbookUses: parameter (body mapped, private, only invoked)
    PackagePattern-->>Fixer: Fixed (sheet is IXLWorksheet)
    Fixer->>FlowTypesPattern: Rewrite Fill on the updated tree
    FlowTypesPattern->>FlowTypesPattern: every caller argument no longer ExcelWorksheet
    FlowTypesPattern-->>Fixer: Fixed (IXLWorksheet parameter, body rewritten)
```

## Figure 3: decision tree

```mermaid
flowchart TD
    Z[field or method found] --> Y{field of an EPPlus type?}
    Y -- yes --> YF{E21: private worksheet field, every write a migrated package's sheet in its scope, other uses Name or later in that scope?}
    YF -- no --> MF[Manual FlowFieldNotMapped]
    YF -- yes --> OKF[Fixed: IXLWorksheet field]
    Y -- no --> X{EPPlus return type?}
    X -- yes --> XR{E22: private, one ExcelPackage parameter, returns its Workbook.Worksheets.Add of a valid literal?}
    XR -- no --> MR[Manual FlowReturnNotMapped]
    XR -- yes --> XC{every caller passes a migrated package?}
    XC -- no --> M4
    XC -- yes --> OKR[Fixed: IXLWorksheet return, XLWorkbook parameter]
    X -- no --> A
    A[method with an ExcelWorksheet parameter] --> B{private, ordinary, non-partial type, has body?}
    B -- no --> M1[Manual FlowMemberNotPrivate]
    B -- yes --> C{one by-value worksheet, no other EPPlus type?}
    C -- no --> M2[Manual FlowUseNotMigrated]
    C -- yes --> D{every reference invoked?}
    D -- no --> M2
    D -- yes --> E{every parameter use mapped by WorkbookUses?}
    E -- no --> M3[Manual: that use's reason]
    E -- yes --> G{E23: linked helpers acyclic, at most 3 deep, each passing these checks?}
    G -- no --> M2
    G -- yes --> F{every caller argument from outside the chain migrated?}
    F -- no --> M4[Manual FlowCallerNotMigrated]
    F -- yes --> OK[Fixed: IXLWorksheet parameter]
```

## See also

- [plan](epplus-flow-types-plan.md), [verify](epplus-flow-types-verify.md)
