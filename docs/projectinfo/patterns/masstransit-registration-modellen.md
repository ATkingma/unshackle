# Models: masstransit.registration

Facts from `src/Unshackle.Core/Fixes/MassTransit/Patterns/RegistrationPattern.cs`, `DetectorPattern.cs` and `MassTransitSyntax.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class DetectorPattern { <<abstract>> Reason; Find(root, model); Rewrite(node, model) Manual }
    class RegistrationPattern
    class MassTransitSyntax { FindCalls; FindClasses; NamesType }
    class MassTransitFix { IFix; IReportOnlyFix }
    IPattern <|.. DetectorPattern
    DetectorPattern <|-- RegistrationPattern
    RegistrationPattern ..> MassTransitSyntax
    MassTransitFix o-- RegistrationPattern
```

## Figure 2: sequence

```mermaid
sequenceDiagram
    Fixer->>ReportOnlyRun: MassTransitFix is IReportOnlyFix
    ReportOnlyRun->>RegistrationPattern: Find(root, model)
    RegistrationPattern->>MassTransitSyntax: feature check
    ReportOnlyRun->>RegistrationPattern: Rewrite(node)
    RegistrationPattern-->>ReportOnlyRun: Manual(reason + guidance)
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
| MassTransit feature | Manual | `Registration/add-mass-transit` |
| own look-alike type | not reported | `FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported` |

## See also

- [plan](masstransit-registration-plan.md), [verify](masstransit-registration-verify.md)
