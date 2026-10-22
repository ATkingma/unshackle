# Verification: masstransit-publish-send

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/masstransit-publish-send-plan.md`](masstransit-publish-send-plan.md) |
| **Status**    | Verified (pending README / `docs/patterns.md` rows, main session)                            |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

> Verified by an AI agent against the code of branch `worktree-agent-a04c16b800e2dab3f` (TAAK-310, council round 11 T6).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `MassTransitFixTests.FixAsync_PublishAndSendOnMassTransitTypes_AreInfo` (`IPublishEndpoint.Publish`, `ISendEndpoint.Send` via `ISendEndpointProvider`, `IBus.Publish`; lines 11, 13, 14) |
| REQ-2 | Met     | `FixAsync_PublishOnOwnOrUnresolvedType_IsNotReported` |
| REQ-3 | Met     | report-only run (`FixAsync_App_ReportsEveryFeatureAsManualAndChangesNothing`, `ReportOnlyFixTests`) |

## Build and tests

- `dotnet build`: 0 warnings. `dotnet test`: 2026 total, 2026 passed.

## Deviations

- No proof file: `ProofTests` has no Info kind (`.info.txt`); requested from the main session. Unit tests use MassTransit 8.5.12.
- Diagrams are inline Mermaid.

## See also

- [plan](masstransit-publish-send-plan.md), [modellen](masstransit-publish-send-modellen.md)
