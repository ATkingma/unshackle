# Verification: mediatr-pipeline-behavior

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-pipeline-behavior-plan.md`](mediatr-pipeline-behavior-plan.md) |
| **Status**    | Verified                                                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                                |

> Verified by an AI agent against the code at uncommitted changes (basis b53e3cc).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs`, `MediatRSyntax.FindHandleMethods` (nu `DescendantNodesAndSelf`), `MediatRFix` (pattern + `IPipelineBehavior\`2`in`SwappedTypes`). Proofs: `src/Unshackle.Tests/Proofs/MediatR/PipelineBehavior/`.

| REQ   | Oordeel | Bewijs                                   |
| ----- | ------- | ---------------------------------------- |
| REQ-1 | Gehaald | `open-async`                             |
| REQ-2 | Gehaald | `no-constraint`, `new-constraint`        |
| REQ-3 | Gehaald | `closed`                                 |
| REQ-4 | Gehaald | `next-token`                             |
| REQ-5 | Gehaald | `next-stored`, `next-passed`, `explicit` |

Elke `after` compileert tegen Mediator 3.0.2 mét de Mediator-source-generator (zie TAAK-027, Keuzes).

Scan op SampleShop (na `dotnet restore`): `mediatr.pipeline-behavior` 2 Fixed, 0 Manual (`LoggingBehavior`, `ValidationBehavior`).

## Deviations

- `SwappedTypesTests.SwappedTypes_PipelineBehavior_IsNotSwappedBecauseItsSignatureDiffers` verwijderd: het pattern dekt het signatuurverschil, dus de naam mag nu swappen.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 400/400 groen                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen                             |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld |

## Vervolg (2026-10-11)

TAAK-081: een constraint die na de swap al `IMessage` is (`IRequest<TResponse>`, `INotification`, of een eigen interface daarop; herkend via MediatR's `IBaseRequest`/`INotification` in `AllInterfaces`) krijgt geen extra `IMessage`. Proof `edge-request-constraint`. Let op: zo'n behavior geldt in Mediator alleen voor `IRequest`-berichten, niet voor `ICommand`/`IQuery` (geen rapportregel, niet gevraagd).

End-to-end (`fix --fix mediatr` op een kopie van SampleShop, na restore): 20 Fixed, 8 Manual; build zonder CS0436 (alleen MSG0003/MSG0005 van de generieke `CountQuery`). Manual: pre/post-processor, exception-handler, stream-behavior (TAAK-061/062), generieke `CountQuery`, en daardoor `AddMediatR` (`AddOpenStreamBehavior`). Omdat de registratie MediatR blijft, falen 28/38 sample-tests bij het opstarten (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); dat lost zich op met TAAK-061/062.

## Bijgewerkt (review B L3, 2026-10-11)

- `CommaList`, de constraint-helper (`MediatRSyntax.WithConstraint`, ook voor `IStreamMessage`) en het expliciete-interface-Manual (`MediatRSyntax.RejectExplicit`) zijn gedeeld; gedrag ongewijzigd.

## Zie ook

- [mediatr-pipeline-behavior-plan.md](mediatr-pipeline-behavior-plan.md)
- [mediatr-pipeline-behavior-modellen.md](mediatr-pipeline-behavior-modellen.md)
