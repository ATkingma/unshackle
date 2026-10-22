# Models: masstransit.request-client

Facts from `src/Unshackle.Core/Fixes/MassTransit/Patterns/RequestClientPattern.cs`, `DetectorPattern.cs` and `MassTransitSyntax.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class DetectorPattern { <<abstract>> Reason; Find(root, model); Rewrite(node, model) Manual }
    class RequestClientPattern
    class MassTransitSyntax { FindCalls; FindClasses; NamesType }
    class MassTransitFix { IFix; IReportOnlyFix }
    IPattern <|.. DetectorPattern
    DetectorPattern <|-- RequestClientPattern
    RequestClientPattern ..> MassTransitSyntax
    MassTransitFix o-- RequestClientPattern
```

## Figure 2: sequence

```mermaid
sequenceDiagram
    Fixer->>ReportOnlyRun: MassTransitFix is IReportOnlyFix
    ReportOnlyRun->>RequestClientPattern: Find(root, model)
    RequestClientPattern->>MassTransitSyntax: feature check
    ReportOnlyRun->>RequestClientPattern: Rewrite(node)
    RequestClientPattern-->>ReportOnlyRun: Manual(reason + guidance)
    ReportOnlyRun-->>Fixer: header row + Manual rows, solution unchanged
```

## Figure 3: decision tree

```mermaid
flowchart TD
    A[node with a feature name] --> B{symbol resolves?}
    B -- yes --> C{in a MassTransit namespace?}
    C -- no --> N[not reported]
    C -- yes --> M[Manual: reason + guidance]
    B -- no --> D{file imports MassTransit?}
    D -- no --> N
    D -- yes --> M
```

| Question | Leaf | Proof / test |
| -------- | ---- | ------------ |
| MassTransit feature | Manual | `RequestClient/request-client-field` |
| own look-alike type | not reported | `FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported` |

## See also

- [plan](masstransit-request-client-plan.md), [verify](masstransit-request-client-verify.md)
