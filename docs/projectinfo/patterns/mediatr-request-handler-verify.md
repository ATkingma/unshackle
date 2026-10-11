# Verification: mediatr-request-handler

|               |                                                                                                |
| ------------- | ---------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-request-handler-plan.md`](mediatr-request-handler-plan.md) |
| **Status**    | Verified                                                                                       |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                            |

> Verified by an AI agent against the code at uncommitted changes (basis ed2e155).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerPattern.cs`, registratie in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/`.

| REQ   | Oordeel | Bewijs                                                |
| ----- | ------- | ----------------------------------------------------- |
| REQ-1 | Gehaald | `async`                                               |
| REQ-2 | Gehaald | `from-result-expression`                              |
| REQ-3 | Gehaald | `from-result-block` (ook `Task.FromResult<int>(...)`) |
| REQ-4 | Gehaald | `generic`                                             |
| REQ-5 | Gehaald | `token-name`                                          |
| REQ-6 | Gehaald | `other-return`, `explicit`                            |
| Uitbreiding TAAK-080 | Gehaald | `edge-marker-interface` |

Regressie-edges: `edge-nested-generic` (geneste generieke response bij REQ-1), `edge-primary-ctor` (primary constructor bij REQ-2).

Scan op SampleShop (`scan samples/SampleShop/SampleShop.slnx --fix mediatr`, na `dotnet restore`): `mediatr.request-handler` 5 Fixed, 0 Manual (`CountQuery`, `RegisterCustomerCommand`, `GetOrderHandler`, `ListOrdersHandler`, `PlaceOrderHandler`); `project-file` Manual voor `SampleShop.csproj` omdat MediatR daar nog gebruikt wordt (notification-handler, `AddMediatR`: latere patronen).

## Deviations

| #   | Area  | Plan says | Code does                                                                                                                                                                         | Impact                                    |
| --- | ----- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| 1   | Tests | —         | `ProofTests` geeft de fix-substitute nu ook `SwappedTypes` (nodig voor `using MediatR` → `using Mediator`); voor `*.manual.txt`-proofs leeg, zodat die alleen het pattern toetsen | None voor FA-proofs (`SwappedTypes` leeg) |

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 318/318 groen                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen                             |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld |

## Vervolg (2026-10-11)

TAAK-080: handlers via een eigen marker-interface (`IAppQueryHandler<TQ, TR> : IRequestHandler<TQ, TR>`) worden gevonden: `MediatRSyntax.FindHandleMethods` kijkt naar `AllInterfaces`. Proof `edge-marker-interface`. `mediatr.unsupported` slaat interface-declaraties over (een generieke marker-interface is geen generieke handler).

End-to-end (`fix --fix mediatr` op een kopie van SampleShop, na restore): 20 Fixed, 8 Manual; build zonder CS0436 (alleen MSG0003/MSG0005 van de generieke `CountQuery`). Manual: pre/post-processor, exception-handler, stream-behavior (TAAK-061/062), generieke `CountQuery`, en daardoor `AddMediatR` (`AddOpenStreamBehavior`). Omdat de registratie MediatR blijft, falen 28/38 sample-tests bij het opstarten (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); dat lost zich op met TAAK-061/062.

## Zie ook

- [mediatr-request-handler-plan.md](mediatr-request-handler-plan.md)
- [mediatr-request-handler-modellen.md](mediatr-request-handler-modellen.md)
