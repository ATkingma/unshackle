---
kingmadoc: 1
feature: "masstransit-consumer"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MassTransit/MassTransitFix.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/ConsumerPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/DetectorPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/MassTransitSyntax.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/MassTransitCode.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/ConsumerFacts.cs
    - src/Unshackle.Tests/Proofs/MassTransit/Consumer/
    - src/Unshackle.Tests/Core/MassTransit/MassTransitFixTests.cs
---

# Feature: masstransit.consumer: report-only detector row

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-310 step 4; council round 8 T1–T5 and S2 (report-only fixes, TAAK-316); round 10 C1 (typed codes); round 11 T7.

## One-sentence summary

masstransit.consumer: every class whose base list names MassTransit `IConsumer<T>` is one Manual row with the feature's reason and guidance for OpenTransit, Wolverine and Rebus; nothing is rewritten.

## Scope (in / out)

**In scope**

- every class whose base list names MassTransit `IConsumer<T>`. A symbol that resolves must be in a `MassTransit` namespace; one that does not resolve is matched by name only in a
  file that imports MassTransit.

**Out of scope**

- Any rewrite or TODO skeleton (T3–T4). Package changes: `MassTransitFix` is an `IReportOnlyFix`, so the Fixer touches no project file.

## Classification (council round 11, T7)

Never Fixed. The first matching row decides; every Manual row carries its `MassTransitCode` (rendered `Code: reason`).

| Consumer shape | Result | Code |
| -------------- | ------ | ---- |
| `IConsumer<T>` does not resolve, or `Consume` is not in the class's file | Manual | `ConsumerNotAnalyzable` |
| consumes `Batch<T>` or `Fault<T>` | Manual | `ConsumerSpecialMessage` |
| a `ConsumerDefinition<C>` for it anywhere in the solution | Manual | `ConsumerWithDefinition` |
| any filter in the solution (`UseFilter`, `UseConsumeFilter`, `UseSendFilter`, `UsePublishFilter`, `UseExecute(Async)`, a class implementing `IFilter<T>`) | Manual (every consumer) | `ConsumerFilter` |
| `context.Headers`, `GetHeader`, `TryGetHeader` | Manual | `ConsumerHeaders` |
| `context.Respond` / `RespondAsync` | Manual | `ConsumerRespond` |
| `context.Redeliver` / `Defer` | Manual | `ConsumerRedeliver` |
| `Publish`, `Send`, `GetSendEndpoint`, `Forward`, `Schedule...`, or an injected `IPublishEndpoint`/`ISendEndpoint(Provider)`/`IBus` | Manual | `ConsumerPublish` |
| the context handed on, any other context member, any other MassTransit symbol in the class | Manual | `ConsumerContextFeature` |
| only `context.Message` and `context.CancellationToken` | Info ("Simple MassTransit consumer of T ...") | — |
| run without the solution (`Rewrite`, not `RewriteAsync`) and otherwise simple | Manual | `ConsumerNotAnalyzable` |

The other detector rows carry one code each: `BusRegistration`, `Transport`, `Outbox`, `RetryPolicy`, `TestHarness`,
`ConsumerDefinition`, `Saga`, `RequestResponse` (C1). Publish/Send outside consumers: `masstransit.publish-send` (T6).

## Design

`ConsumerPattern` derives from `DetectorPattern` (Template Method): `Find` is the feature check through `MassTransitSyntax`, `Rewrite`
always returns `Manual(Reason + Guidance)`. The report-only run (TAAK-316) adds the `inventory-only` header row and never counts a row as Fixed.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | every class whose base list names MassTransit `IConsumer<T>`: Manual with the feature reason (proof `Consumer/consumer`). |
| REQ-2 | The code and project files stay as they are; the report says "inventory only" (`FixAsync_ReasonGivesTargetGuidance`, `FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported`). |
| REQ-3 | Code that only looks like MassTransit (own types outside the `MassTransit` namespace) is not reported (`FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported`). |
| REQ-4 | (T7) A consumer using only `context.Message` and cancellation, with no definition or filter in the solution, is an Info row. |
| REQ-5 | (T7) Every other consumer is Manual with the code of the first feature in the classification table. |
| REQ-6 | (C1) Every detector Manual row carries a typed `MassTransitCode`. |

## See also

- [verify](masstransit-consumer-verify.md), [modellen](masstransit-consumer-modellen.md)
- `claude/tasks/TAAK-310-masstransit-v8.md`, `docs/projectinfo/architectuur/corpus-masstransit.md`
