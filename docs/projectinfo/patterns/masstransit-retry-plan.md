---
kingmadoc: 1
feature: "masstransit-retry"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/MassTransit/MassTransitFix.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/RetryPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/DetectorPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/MassTransitSyntax.cs
    - src/Unshackle.Tests/Proofs/MassTransit/Retry/
    - src/Unshackle.Tests/Core/MassTransit/MassTransitFixTests.cs
---

# Feature: masstransit.retry: report-only detector row

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-310 step 4; council round 8 T1–T5 and S2 (report-only fixes, TAAK-316).

## One-sentence summary

masstransit.retry: every retry/redelivery call (`UseMessageRetry`, `UseRetry`, `UseDelayedRedelivery`, `UseScheduledRedelivery`, `UseKillSwitch`, `UseCircuitBreaker`) is one Manual row with the feature's reason and guidance for OpenTransit, Wolverine and Rebus; nothing is rewritten.

## Scope (in / out)

**In scope**

- every retry/redelivery call (`UseMessageRetry`, `UseRetry`, `UseDelayedRedelivery`, `UseScheduledRedelivery`, `UseKillSwitch`, `UseCircuitBreaker`). A symbol that resolves must be in a `MassTransit` namespace; one that does not resolve is matched by name only in a
  file that imports MassTransit.

**Out of scope**

- Any rewrite or TODO skeleton (T3–T4). Package changes: `MassTransitFix` is an `IReportOnlyFix`, so the Fixer touches no project file.

## Design

`RetryPattern` derives from `DetectorPattern` (Template Method): `Find` is the feature check through `MassTransitSyntax`, `Rewrite`
always returns `Manual(Reason + Guidance)`. The report-only run (TAAK-316) adds the `inventory-only` header row and never counts a row as Fixed.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | every retry/redelivery call (`UseMessageRetry`, `UseRetry`, `UseDelayedRedelivery`, `UseScheduledRedelivery`, `UseKillSwitch`, `UseCircuitBreaker`): Manual with the feature reason (proof `Retry/message-retry`). |
| REQ-2 | The code and project files stay as they are; the report says "inventory only" (`FixAsync_App_ReportsEveryFeatureAsManualAndChangesNothing`). |
| REQ-3 | Code that only looks like MassTransit (own types outside the `MassTransit` namespace) is not reported (`FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported`). |

## See also

- [verify](masstransit-retry-verify.md), [modellen](masstransit-retry-modellen.md)
- `claude/tasks/TAAK-310-masstransit-v8.md`, `docs/projectinfo/architectuur/corpus-masstransit.md`
