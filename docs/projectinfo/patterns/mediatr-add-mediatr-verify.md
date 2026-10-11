# Verification: mediatr-add-mediatr

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`](mediatr-add-mediatr-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                    |

> Verified by an AI agent against the code at uncommitted changes (basis b53e3cc).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`, registratie in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/AddMediatR/`.

| REQ   | Oordeel | Bewijs                              |
| ----- | ------- | ----------------------------------- |
| REQ-1 | Gehaald | `behaviors`                         |
| REQ-2 | Gehaald | `expression`                        |
| REQ-3 | Gehaald | `unknown-call`, `behavior-lifetime` |
| REQ-4 | Gehaald | `publisher`                         |
| REQ-5 | Gehaald | `no-lambda`                         |
| REQ-6 | Gehaald | `generic-handlers` (TAAK-037)       |

`after` compileert alleen met de Mediator-source-generator (die schrijft `AddMediator`); de proof-runner draait hem (`ProofFix.AfterGenerators`). De Fixer voegt `using Mediator;` toe, ook al is hij in `behaviors`/`expression` ongebruikt (onschuldig).

Scan op SampleShop (na `dotnet restore`): `mediatr.add-mediatr` 0 Fixed, 1 Manual: `'cfg.RegisterGenericHandlers = true' has no direct Mediator equivalent` (`ServiceCollectionExtensions.cs:20`; daarna ook stream-behavior, pre/post-processors en publisher, die allemaal Manual zijn).

Na TAAK-037 valt `RegisterGenericHandlers` weg; de Manual op SampleShop is nu `cfg.NotificationPublisher = new TaskWhenAllPublisher()` (en daarna stream-behavior en pre/post-processors).

## Deviations

Geen.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 400/400 groen                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen                             |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld |

## Zie ook

- [mediatr-add-mediatr-plan.md](mediatr-add-mediatr-plan.md)
- [mediatr-add-mediatr-modellen.md](mediatr-add-mediatr-modellen.md)
