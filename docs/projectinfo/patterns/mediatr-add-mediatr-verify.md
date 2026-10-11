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

## Vervolg (2026-10-11)

TAAK-060: `cfg.NotificationPublisher = new TaskWhenAllPublisher()` / `new ForeachAwaitPublisher()` (ook MediatR-gekwalificeerd) → `cfg.NotificationPublisherType = typeof(global::Mediator.X)` (Mediator 3.0.2: `Mediator.TaskWhenAllPublisher`, `Mediator.ForeachAwaitPublisher`, `MediatorOptions.NotificationPublisherType`). `global::` bindt tijdens de fix niet aan MediatR, dus `using MediatR.NotificationPublishers;` (nu in `RemovedNamespaces`) verdwijnt. Blijft één removed using staan (bv. AddMediatR Manual), dan blijven ze allemaal (`UsingDirectives.UsedRemovedNamespaces`): geen CS0104/CS0029. Proofs `publisher` (was manual) en `edge-publisher-qualified`. Eigen publisher blijft Manual (`mediatr.unsupported`).

TAAK-063: `Mediator.SourceGenerator` alleen in projecten die `AddMediator`/`AddMediatR` aanroepen (`AddedPackage.OnlyWithCallTo`, syntactisch in `ProjectPackages`), met `PrivateAssets="all"` zodat hij niet transitief naar testprojecten stroomt (dat gaf de CS0436).

End-to-end (`fix --fix mediatr` op een kopie van SampleShop, na restore): 20 Fixed, 8 Manual; build zonder CS0436 (alleen MSG0003/MSG0005 van de generieke `CountQuery`). Manual: pre/post-processor, exception-handler, stream-behavior (TAAK-061/062), generieke `CountQuery`, en daardoor `AddMediatR` (`AddOpenStreamBehavior`). Omdat de registratie MediatR blijft, falen 28/38 sample-tests bij het opstarten (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); dat lost zich op met TAAK-061/062.

## Bijgewerkt (TAAK-061/062, review B M3/M4, 2026-10-11)

- `AddOpenRequestPreProcessor`/`AddRequestPreProcessor` → `PipelineBehaviors` vóór de behaviors (open: `typeof(X<>)` → `typeof(X<,>)`), post-processors erna; gesloten exception handlers en processors uit het eigen project (MediatR-assembly-scan) vooraan. `AddOpenStreamBehavior`/`AddStreamBehavior` → `StreamPipelineBehaviors`. Proof `processors`.
- M3: nieuwe statements krijgen alleen de indentatie; comments van alle oude statements komen vóór de eerste (proof `processors`).
- M4: leeg blok `cfg => { }` → `cfg => cfg.ServiceLifetime = ServiceLifetime.Transient` (proof `empty`).
- `Find` accepteert ook een candidate-symbool: nadat een ander bestand al herschreven is bindt de lambda niet meer schoon (gevonden met de SampleShop-integratietest).
- `unknown-call` gebruikt nu `AddOpenBehaviors(...)` (de pre-processor wordt omgezet).

## Zie ook

- [mediatr-add-mediatr-plan.md](mediatr-add-mediatr-plan.md)
- [mediatr-add-mediatr-modellen.md](mediatr-add-mediatr-modellen.md)
