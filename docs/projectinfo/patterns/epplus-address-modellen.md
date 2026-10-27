# Models: epplus.address

Facts from `src/Unshackle.Core/Fixes/EPPlus/Patterns/AddressPattern.cs` and `src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class AddressPattern { Find(root, model); Rewrite(node, model) }
    class WorkbookUses { MapCellUse(use); IsAddressRead(range); IsCovered(value) }
    class DimensionMapping { IsRewrittenName(name, model) }
    IPattern <|.. AddressPattern
    EPPlusFix o-- AddressPattern
    WorkbookUses ..> AddressPattern : accepts its tail
    UnsupportedPattern ..> DimensionMapping : skips RowCount/RangeAddress/...
```

## Figure 2: sequence (full fix)

```mermaid
sequenceDiagram
    Fixer->>AddressPattern: Rewrite ws.Cells[2, 1, last, 1].Rows
    AddressPattern-->>Fixer: Fixed ws.Cells[2, 1, last, 1].RowCount()
    Fixer->>PackagePattern: Rewrite package scope
    PackagePattern->>WorkbookUses: MapCellUse (range under RowCount())
    WorkbookUses-->>PackagePattern: ws.Range(2, 1, last, 1)
    PackagePattern-->>Fixer: Fixed (ws.Range(..).RowCount())
```

## Figure 3: decision tree

```mermaid
flowchart TD
    A[ws.Cells-range member Rows/Columns/Start/End] --> B{Rows, Columns, or Start/End followed by Row/Column?}
    B -- no --> M[Manual UnmappedAddress]
    B -- yes --> C{four ints, or literal A1:C2 in order?}
    C -- no --> M
    C -- yes --> OK[Fixed: RowCount / ColumnCount / RangeAddress.First/LastAddress]
```

## See also

- [plan](epplus-address-plan.md), [verify](epplus-address-verify.md)
