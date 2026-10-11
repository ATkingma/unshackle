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
| REQ-1 | Vervallen | eigen patroon `mediatr.request-processor` (TAAK-061) |
| REQ-2 | Vervallen | eigen patroon `mediatr.request-processor` (TAAK-061) |
| REQ-3 | Gehaald (alleen `IRequestExceptionAction`) | `exception-action`; `IRequestExceptionHandler` → `mediatr.exception-handler` |
| REQ-4 | Gehaald | `notification-publisher` |
| REQ-5 | Vervallen | eigen patroon `mediatr.stream-pipeline-behavior` (TAAK-062) |
| REQ-6 | Gehaald | `generic-handler`, `derived-generic-message` |

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

## Bijgewerkt (TAAK-061/062, review B M5, 2026-10-11)

- Pre/post-processors, exception handlers en stream behaviors zijn eigen patronen (`mediatr.request-processor`, `mediatr.exception-handler`, `mediatr.stream-pipeline-behavior`); proofs `pre-processor`, `post-processor`, `stream-behavior` weg, `exception-handler` → `exception-action`.
- M5: advies alleen op het type dat de interface zelf declareert; een generieke message wordt ook gemeld als `IRequest` via een basistype komt (`AllInterfaces`, proof `derived-generic-message`).

## Zie ook

- [mediatr-unsupported-plan.md](mediatr-unsupported-plan.md)
- [mediatr-unsupported-modellen.md](mediatr-unsupported-modellen.md)
