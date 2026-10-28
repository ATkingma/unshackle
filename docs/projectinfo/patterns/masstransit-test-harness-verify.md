# Verification: masstransit-test-harness

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/masstransit-test-harness-plan.md`](masstransit-test-harness-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a516572e1f4f2f6d6` (TAAK-310 step 4).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `MassTransit/TestHarness/add-test-harness` (`ProofTests`) |
| REQ-2 | Met     | `FixAsync_SagaAndTestHarness_AreReported`; `ReportOnlyFixTests` (TAAK-316) |
| REQ-3 | Met     | `MassTransitFixTests.FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all green (see TAAK-310 progress log).
- No corpus run yet (the 24-repo corpus of `corpus-masstransit.md` is for a later round).
- Council round 24 R2 (2026-10-28): every Manual row and the report-only summary say "pin MassTransit < 9; 9.x is commercial" (`MassTransitFixTests.FixAsync_ReasonGivesTargetGuidance`, `ReportOnlyReason_SaysPinBelowNine`).

## Deviations

- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](masstransit-test-harness-plan.md), [modellen](masstransit-test-harness-modellen.md)
