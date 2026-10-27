# Models: epplus.flow-types

Facts from `src/Unshackle.Core/Fixes/EPPlus/Patterns/FlowTypesPattern.cs` and `src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class FlowTypesPattern { Find(root, model); Rewrite(node, model); Migratable(method, model) }
    class WorkbookUses { WorkbookUses(model, scope, IParameterSymbol); Map(); Replacements }
    class PackagePattern
    IPattern <|.. FlowTypesPattern
    FlowTypesPattern ..> WorkbookUses
    PackagePattern ..> WorkbookUses
    WorkbookUses ..> FlowTypesPattern : IsFlowArgument → Migratable
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
    Y -- yes --> MF[Manual FlowFieldNotMapped]
    Y -- no --> X{EPPlus return type?}
    X -- yes --> MR[Manual FlowReturnNotMapped]
    X -- no --> A
    A[method with an ExcelWorksheet parameter] --> B{private, ordinary, non-partial type, has body?}
    B -- no --> M1[Manual FlowMemberNotPrivate]
    B -- yes --> C{one by-value worksheet, no other EPPlus type?}
    C -- no --> M2[Manual FlowUseNotMigrated]
    C -- yes --> D{every reference invoked?}
    D -- no --> M2
    D -- yes --> E{every parameter use mapped by WorkbookUses?}
    E -- no --> M3[Manual: that use's reason]
    E -- yes --> F{every caller argument migrated?}
    F -- no --> M4[Manual FlowCallerNotMigrated]
    F -- yes --> OK[Fixed: IXLWorksheet parameter]
```

## See also

- [plan](epplus-flow-types-plan.md), [verify](epplus-flow-types-verify.md)
