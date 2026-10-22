---
kingmadoc: 1
feature: "masstransit-transport"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/MassTransit/MassTransitFix.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/TransportPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/DetectorPattern.cs
    - src/Unshackle.Core/Fixes/MassTransit/Patterns/MassTransitSyntax.cs
    - src/Unshackle.Tests/Proofs/MassTransit/Transport/
    - src/Unshackle.Tests/Core/MassTransit/MassTransitFixTests.cs
---

# Feature: masstransit.transport: report-only detector row

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-310 step 4; council round 8 T1–T5 and S2 (report-only fixes, TAAK-316).

## One-sentence summary

masstransit.transport: every `UsingRabbitMq`/`UsingAzureServiceBus`/`UsingAmazonSqs`/`UsingInMemory`/... call, also unresolved (transport package not in the model) in a file that imports MassTransit is one Manual row with the feature's reason and guidance for OpenTransit, Wolverine and Rebus; nothing is rewritten.

## Scope (in / out)

**In scope**

- every `UsingRabbitMq`/`UsingAzureServiceBus`/`UsingAmazonSqs`/`UsingInMemory`/... call, also unresolved (transport package not in the model) in a file that imports MassTransit. A symbol that resolves must be in a `MassTransit` namespace; one that does not resolve is matched by name only in a
  file that imports MassTransit.

**Out of scope**

- Any rewrite or TODO skeleton (T3–T4). Package changes: `MassTransitFix` is an `IReportOnlyFix`, so the Fixer touches no project file.

## Design

`TransportPattern` derives from `DetectorPattern` (Template Method): `Find` is the feature check through `MassTransitSyntax`, `Rewrite`
always returns `Manual(Reason + Guidance)`. The report-only run (TAAK-316) adds the `inventory-only` header row and never counts a row as Fixed.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | every `UsingRabbitMq`/`UsingAzureServiceBus`/`UsingAmazonSqs`/`UsingInMemory`/... call, also unresolved (transport package not in the model) in a file that imports MassTransit: Manual with the feature reason (proof `Transport/using-in-memory`). |
| REQ-2 | The code and project files stay as they are; the report says "inventory only" (`FixAsync_UnresolvedTransportInMassTransitFile_IsMatchedByName`). |
| REQ-3 | Code that only looks like MassTransit (own types outside the `MassTransit` namespace) is not reported (`FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported`). |

## See also

- [verify](masstransit-transport-verify.md), [modellen](masstransit-transport-modellen.md)
- `claude/tasks/TAAK-310-masstransit-v8.md`, `docs/projectinfo/architectuur/corpus-masstransit.md`
