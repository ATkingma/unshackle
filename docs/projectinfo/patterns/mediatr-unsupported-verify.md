# Verification: mediatr-unsupported

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-unsupported-plan.md`](mediatr-unsupported-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                    |

> Verified by an AI agent against the code at uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`, registratie en `SwappedTypes` (`IStreamRequest`1`, `IStreamRequestHandler`2`) in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/Unsupported/`; stream-handler: `SwappedTypesTests.FixAsync_StreamRequestHandler_CompilesAgainstMediator`.

| REQ   | Oordeel | Bewijs                   |
| ----- | ------- | ------------------------ |
| REQ-1 | Gehaald | `pre-processor`          |
| REQ-2 | Gehaald | `post-processor`         |
| REQ-3 | Gehaald | `exception-handler`      |
| REQ-4 | Gehaald | `notification-publisher` |
| REQ-5 | Gehaald | `stream-behavior`        |
| REQ-6 | Gehaald | `generic-handler`        |

Scan op SampleShop: 6 Manual (pre- en post-processor, stream-behavior, exception-handler, `CountQuery<TEntity>`, `CountHandler<TEntity>`). Met deze adviezen met de hand opgelost op een kopie bouwt SampleShop en slagen 38/38 tests (TAAK-037).

## Deviations

- Rij 7 noemde `IStreamRequest`-handlers en `Send(object)` als Manual; beide hebben een tweeling in Mediator en zijn dus geen Manual.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 458/458 groen                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen                             |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld |

## Zie ook

- [mediatr-unsupported-plan.md](mediatr-unsupported-plan.md)
- [mediatr-unsupported-modellen.md](mediatr-unsupported-modellen.md)
