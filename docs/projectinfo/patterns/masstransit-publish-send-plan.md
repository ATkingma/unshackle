---
kingmadoc: 1
feature: "masstransit-publish-send"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/MassTransit/MassTransitFix.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/PublishSendPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/MassTransitSyntax.cs
    - src/Unshackle.Tests/Core/MassTransit/MassTransitFixTests.cs
---

# Feature: masstransit.publish-send: report-only Info row per Publish/Send

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-310; council round 11, T6.

## One-sentence summary

masstransit.publish-send: every `x.Publish(...)` / `x.Send(...)` that resolves to a MassTransit method on a receiver that is (or implements)
`IPublishEndpoint`, `ISendEndpoint`, `ISendEndpointProvider`, `ConsumeContext` or `IBus` is one Info row with Wolverine/Rebus guidance.

## Scope (in / out)

**In scope**

- Member-access invocations named `Publish` or `Send` whose symbol resolves (no candidate symbols) to a method in a `MassTransit`
  namespace, with a receiver type from the list above (itself or one of its interfaces, also in a `MassTransit` namespace).

**Out of scope**

- Name-only matches: an unresolved call, an own `Publish` method, a receiver outside the list. Never reported.
- Any rewrite (T3–T4); the row is Info, never Fixed or Manual.

## Design

`PublishSendPattern` implements `IPattern` directly (not `DetectorPattern`, whose rows are Manual): `Find` keeps the invocations whose
`Receiver` resolves to a listed type; `Rewrite` returns `PatternResult.Note` naming the verb and the receiver type. Registered in
`MassTransitFix` after `RequestClientPattern`.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | A resolved MassTransit `Publish`/`Send` on a listed receiver type is an Info row naming verb and receiver. |
| REQ-2 | A name-only match (own type, unresolved receiver) is not reported. |
| REQ-3 | The code stays as it is (report-only fix). |

## See also

- [verify](masstransit-publish-send-verify.md), [modellen](masstransit-publish-send-modellen.md)
- [masstransit-consumer plan](masstransit-consumer-plan.md) (classification table, T7)
- `claude/tasks/TAAK-310-masstransit-v8.md`, `claude/docs/council.md` round 11
