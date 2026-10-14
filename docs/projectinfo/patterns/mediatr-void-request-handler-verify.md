# Verification: mediatr-void-request-handler

|               |                                                                                                          |
| ------------- | -------------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-void-request-handler-plan.md`](mediatr-void-request-handler-plan.md) |
| **Status**    | Verified                                                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                                      |

> Verified by an AI agent against the code at uncommitted changes (basis c672771).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/VoidRequestHandlerPattern.cs`, helper `MediatRSyntax.cs`, registratie in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/VoidRequestHandler/`.

| REQ   | Verdict | Evidence                                         |
| ----- | ------- | ---------------------------------------------- |
| REQ-1 | Met | `async`                                        |
| REQ-2 | Met | `early-return`                                 |
| REQ-3 | Met | `throw-at-end`                                 |
| REQ-4 | Met | `completed-task`, `completed-task-expression`  |
| REQ-5 | Met | `other-return`, `async-expression`, `explicit` |
| REQ-6 | Met | `_Combined/generic-decorator-marker-without-using` (project `Handlers` gets `using Mediator;`) |
| REQ-7 | Met | `block-end` |
| Extension TAAK-080 | Met | `edge-marker-interface` |

Mediator 3.0.2 bevat `IRequestHandler\`1`en`Unit.ValueTask`(assembly); de`after`-proofs compileren ertegen.
Scan on SampleShop (after `dotnet restore`): `mediatr.void-request-handler` 1 Fixed, 0 Manual (`CancelOrderHandler`).

## Deviations

| #   | Area | Plan says | Code does                                                                                                                    | Impact                                 |
| --- | ---- | --------- | ---------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- |
| 1   | Code | —         | Finding, `ToValueTask`, `ReturnedExpressions` and `IsTaskMember` moved to `MediatRSyntax`; `RequestHandlerPattern` uses them too | None (proofs `RequestHandler/*` green) |

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 381/381 green |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## Follow-up (2026-10-11)

TAAK-080: `IAppCommandHandler<T> : IRequestHandler<T>` is found (`AllInterfaces`). Proof `edge-marker-interface`.

End-to-end (`fix --fix mediatr` on a copy of SampleShop, after restore): 20 Fixed, 8 Manual; build without CS0436 (only MSG0003/MSG0005 of the generic `CountQuery`). Manual: pre/post processor, exception handler, stream behavior (TAAK-061/062), generic `CountQuery`, and because of that `AddMediatR` (`AddOpenStreamBehavior`). Because the registration stays MediatR, 28/38 sample tests fail at startup (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); that resolves itself with TAAK-061/062.

## See also

- [mediatr-void-request-handler-plan.md](mediatr-void-request-handler-plan.md)
- [mediatr-void-request-handler-modellen.md](mediatr-void-request-handler-modellen.md)
