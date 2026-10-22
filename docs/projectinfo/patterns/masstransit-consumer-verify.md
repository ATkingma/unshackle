# Verification: masstransit-consumer

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/masstransit-consumer-plan.md`](masstransit-consumer-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a516572e1f4f2f6d6` (TAAK-310 step 4).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `MassTransit/Consumer/consumer` (`ProofTests`; since T7 a headers consumer) |
| REQ-2 | Met     | `FixAsync_ReasonGivesTargetGuidance`, `FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported`; `ReportOnlyFixTests` (TAAK-316) |
| REQ-3 | Met     | `MassTransitFixTests.FixAsync_OwnIConsumerOutsideMassTransit_IsNotReported` |
| REQ-4 | Met     | theory `FixAsync_SimpleConsumer_IsInfo` (`Task.CompletedTask`; `context.Message` + `context.CancellationToken`) |
| REQ-5 | Met     | proofs `Consumer/consumer` (headers), `consumer-respond`, `consumer-publish`; theory `FixAsync_ConsumerWithFeature_IsManualWithFeatureCode` (headers, respond, redeliver, publish, context handed on, other member); `FixAsync_ConsumerWithInjectedPublishEndpoint_IsManualPublish`; `FixAsync_DefinitionOrFilterElsewhere_KeepsSimpleConsumerManual`; `FixAsync_FaultConsumer_IsManualSpecialMessage` |
| REQ-6 | Met     | `FixAsync_App_EveryManualRowHasATypedCode` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all green (see TAAK-310 progress log).
- No corpus run yet (the 24-repo corpus of `corpus-masstransit.md` is for a later round).
- Round 11 (T6/T7, C1), branch `worktree-agent-a04c16b800e2dab3f`: build 0 warnings; `dotnet test` 2026 total, 2026 passed.

## Deviations

- Diagrams are inline Mermaid instead of D2/PNG under `img/`.
- No proof for the Info leaf: `ProofTests` knows only `.after.cs` and `.manual.txt` (an `.info.txt` kind is a request to the main session).
- Filters and definitions are looked up in the loaded solution only; a filter registered in a project outside it is not seen.

## See also

- [plan](masstransit-consumer-plan.md), [modellen](masstransit-consumer-modellen.md)
