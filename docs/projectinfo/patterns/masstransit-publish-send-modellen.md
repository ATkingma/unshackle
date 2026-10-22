# Models: masstransit.publish-send

Facts from `src/Unshackle.Core/Fixes/MassTransit/Patterns/PublishSendPattern.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class PublishSendPattern { Find(root, model); Rewrite(node, model) Info; Receiver(invocation, model) }
    class MassTransitSyntax { IsMassTransit }
    class MassTransitFix { IFix; IReportOnlyFix }
    IPattern <|.. PublishSendPattern
    PublishSendPattern ..> MassTransitSyntax
    MassTransitFix o-- PublishSendPattern
```

## Figure 2: sequence

```mermaid
sequenceDiagram
    ReportOnlyRun->>PublishSendPattern: Find(root, model)
    PublishSendPattern->>PublishSendPattern: Receiver(invocation) per x.Publish / x.Send
    ReportOnlyRun->>PublishSendPattern: Rewrite(node)
    PublishSendPattern-->>ReportOnlyRun: Info(verb, receiver, guidance)
```

## Figure 3: decision tree

```mermaid
flowchart TD
    A[x.Publish / x.Send] --> B{symbol resolves to a MassTransit method?}
    B -- no --> N[not reported]
    B -- yes --> C{x is or implements IPublishEndpoint, ISendEndpoint, ISendEndpointProvider, ConsumeContext, IBus?}
    C -- no --> N
    C -- yes --> I[Info row]
```

| Leaf | Test |
| ---- | ---- |
| Info row | `FixAsync_PublishAndSendOnMassTransitTypes_AreInfo` |
| not reported | `FixAsync_PublishOnOwnOrUnresolvedType_IsNotReported` |

## See also

- [plan](masstransit-publish-send-plan.md), [verify](masstransit-publish-send-verify.md)
