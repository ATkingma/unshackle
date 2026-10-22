# Models: epplus.styles

Facts from `src/Unshackle.Core/Fixes/EPPlus/Patterns/StylesPattern.cs` and `StylesMapping.cs`, and the harness in
`src/Unshackle.Tests/Core/EPPlus/` (`StylesHarnessTests`, `CellCompare`).

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class StylesPattern { MigratedAnnotationKind; Find(root, model); Rewrite(node, model) }
    class StylesMapping { Split(expr, model); Assignment(shape, value, model); EnumMember(value, model, enum) }
    class UnsupportedPattern
    IPattern <|.. StylesPattern
    StylesPattern ..> StylesMapping
    EPPlusFix o-- StylesPattern
    EPPlusFix o-- UnsupportedPattern
```

## Figure 2: sequence (harness case)

```mermaid
sequenceDiagram
    StylesHarnessTests->>Fixer: EPPlus program, StylesPattern only
    Fixer->>StylesPattern: Find → style statements; Rewrite each
    StylesPattern-->>Fixer: Fixed(ClosedXML statement, annotated)
    StylesHarnessTests->>StylesHarnessTests: translate scaffold (package, sheet, cell, save)
    StylesHarnessTests->>CellCompare: Differences(before, after) == []
    StylesHarnessTests->>CellCompare: Differences(before, wrong after) != []
```

## Figure 3: decision tree

```mermaid
flowchart TD
    A[statement starting from an ExcelStyle] --> B{Fill.PatternType?}
    B -- yes --> C{Solid and next is BackgroundColor.SetColor on same S?}
    C -- yes --> R[Fixed: removed]
    C -- no --> MF[Manual FillWithoutSolidPattern]
    B -- no --> D{Fill.BackgroundColor.SetColor?}
    D -- yes --> E{previous is PatternType = Solid on same S?}
    E -- yes --> F1[Fixed: BackgroundColor = XLColor.FromColor]
    E -- no --> MF
    D -- no --> G{mapped plain path, Font.Color.SetColor, or enum path with literal mapped member?}
    G -- yes --> F2[Fixed: ClosedXML statement]
    G -- no --> MU[Manual UnmappedEPPlusStyle]
```

| Question | Leaf | Proof / test |
| -------- | ---- | ------------ |
| font, number format, wrap, alignment, borders | Fixed, harness equal and wrong-style detected | `StylesHarnessTests` (15 cases) |
| solid fill + colour | Fixed, harness equal | `StylesHarnessTests` fill case |
| pattern without colour | Manual | `fill-without-color`, `FillPatternWithoutColor_IsManual` |
| `UnderLine`, `BorderAround`, variable alignment | Manual | `underline`, `border-around`, `alignment-variable` |

## See also

- [plan](epplus-styles-plan.md), [verify](epplus-styles-verify.md)
