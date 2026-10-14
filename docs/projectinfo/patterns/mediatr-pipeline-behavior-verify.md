# Verification: mediatr-pipeline-behavior

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-pipeline-behavior-plan.md`](mediatr-pipeline-behavior-plan.md) |
| **Status**    | Verified                                                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                                |

> Verified by an AI agent against the code at uncommitted changes (basis b53e3cc).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs`, `MediatRSyntax.FindHandleMethods` (now `DescendantNodesAndSelf`), `MediatRFix` (pattern + `IPipelineBehavior\`2`in`SwappedTypes`). Proofs: `src/Unshackle.Tests/Proofs/MediatR/PipelineBehavior/`.

| REQ   | Verdict | Evidence                                 |
| ----- | ------- | ---------------------------------------- |
| REQ-1 | Met     | `open-async`                             |
| REQ-2 | Met     | `no-constraint`, `new-constraint`        |
| REQ-3 | Met     | `closed`                                 |
| REQ-4 | Met     | `next-token`                             |
| REQ-5 | Met     | `next-stored`, `next-passed`, `explicit` |
| Extension TAAK-081 | Met | `edge-request-constraint`, `edge-notnull-constraint` |

Every `after` compiles against Mediator 3.0.2 with the Mediator source generator (see TAAK-027, Choices).

Scan on SampleShop (after `dotnet restore`): `mediatr.pipeline-behavior` 2 Fixed, 0 Manual (`LoggingBehavior`, `ValidationBehavior`).

## Deviations

- `SwappedTypesTests.SwappedTypes_PipelineBehavior_IsNotSwappedBecauseItsSignatureDiffers` removed: the pattern covers the signature difference, so the name may now swap.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 400/400 green                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green                             |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## Follow-up (2026-10-11)

TAAK-081: a constraint that is already `IMessage` after the swap (`IRequest<TResponse>`, `INotification`, or an own interface on top of it; recognized via MediatR's `IBaseRequest`/`INotification` in `AllInterfaces`) gets no extra `IMessage`. Proof `edge-request-constraint`. Note: in Mediator such a behavior only applies to `IRequest` messages, not to `ICommand`/`IQuery` (no report line, not requested).

End-to-end (`fix --fix mediatr` on a copy of SampleShop, after restore): 20 Fixed, 8 Manual; build without CS0436 (only MSG0003/MSG0005 from the generic `CountQuery`). Manual: pre/post-processor, exception handler, stream behavior (TAAK-061/062), generic `CountQuery`, and as a result `AddMediatR` (`AddOpenStreamBehavior`). Because the registration stays MediatR, 28/38 sample tests fail at startup (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); this resolves with TAAK-061/062.

## Updated (review B L3, 2026-10-11)

- `CommaList`, the constraint helper (`MediatRSyntax.WithConstraint`, also for `IStreamMessage`) and the explicit-interface Manual (`MediatRSyntax.RejectExplicit`) are shared; behavior unchanged.

## See also

- [mediatr-pipeline-behavior-plan.md](mediatr-pipeline-behavior-plan.md)
- [mediatr-pipeline-behavior-modellen.md](mediatr-pipeline-behavior-modellen.md)
