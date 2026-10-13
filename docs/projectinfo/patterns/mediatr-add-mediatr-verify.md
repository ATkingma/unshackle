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
| REQ-4 | Gehaald | eigen publisher (geen proof; `publisher` is sinds TAAK-060 Fixed) |
| REQ-5 | Gehaald | `no-lambda`                         |
| REQ-6 | Gehaald | `generic-handlers` (TAAK-037)       |
| REQ-7 | Gehaald | `MediatorHostsTests` (AddMediatR-host naast host; scan van andere host → Manual; TAAK-213) |
| Uitbreiding TAAK-060 | Gehaald | `publisher`, `edge-publisher-qualified` |
| Uitbreiding TAAK-061/062 | Gehaald | `processors`, `empty` |
| Uitbreiding TAAK-144 | Gehaald | `edge-assemblies-public`, `AddMediatRPatternTests` |
| Uitbreiding TAAK-183 | Gehaald | `AddMediatRPatternTests.Rewrite_ScannedAssemblyOfOtherProject_ManualWhenHandlerNotPublicAndNoFriend` |
| Uitbreiding TAAK-147 | Gehaald | `edge-assemblies-friend`, `AddMediatRPatternTests`, `SwappedTypesTests` |
| Uitbreiding TAAK-195 | Gehaald | `_Combined/add-mediatr-internal-handlers-other-project`, `InternalHandlerAccessPatternTests` |
| Uitbreiding TAAK-240 | Gehaald | `AddMediatRPatternTests.RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace`; scan kgrzybek |
| Uitbreiding TAAK-148 | Gehaald | `edge-assemblies-friend` (nu Fixed), `AddMediatRPatternTests.RewriteAsync_InternalMethodWithFriend_ManualOnlyWhenOtherProjectCallsIt` |

TAAK-117: een parameter die alleen naar een weggevallen `RegisterServicesFrom...` ging (IDE0060) blijft staan. TAAK-141: is de aanroep een los statement, dan komt er `_ = parameter;` vóór (geen IDE0060, signatuur en aanroepers ongewijzigd; proof `edge-assemblies`, evo1 bouwt weer). Anders `Fixed` met een waarschuwing in het rapport (`AddMediatRPatternTests`).

TAAK-144: is die methode publiek (evo2's gedeelde `MediatorModule.AddMediator(services, assembly)`), of scant de aanroep een ander project met een internal handler, dan Manual; proof `edge-assemblies` is daarom nu `internal`. Daarnaast krijgt een eigen type dat zo heet als een Mediator-type (`ICommand`) de volledige naam waar `using Mediator;` binnenkomt (`IFix.AddedTypes`, proof `RequestHandler/edge-own-command-name`). Harnas: evo2 `ok` (Manual, ongewijzigd, 39/39), evo1 `ok` (45/45).

TAAK-183: probe met Mediator 3.0.2 (host + library met public en internal handler): zonder `InternalsVisibleTo` CS0122 in `Mediator.g.cs`, met of zonder `o.Assemblies = [typeof(Marker)]`; met `InternalsVisibleTo("Host")` bouwt en draait het (beide handlers). Een internal handler in een assembly die dit project als friend heeft, is dus geen Manual meer. ardalis (geen friend, plus Ardalis.SharedKernel), evo1 (testproject als aanroeper) en evo2 (publieke methode met `Assembly`-parameter, internal handlers zonder friend) blijven terecht Manual.

TAAK-195: een internal handler in een project van de solution is geen Manual meer; `mediatr.internal-handler-access` voegt `InternalsVisibleTo` toe (proof `_Combined/add-mediatr-internal-handlers-other-project`; ardalis-clone: 0 Manual, Web bouwt). evo1 en evo2 blijven Manual; gemeten (Mediator 3.0.2): een notification die de host-generator niet kent wordt door `Publish` stil genegeerd (ook als de handler in DI staat), en een tweede generator in een testproject geeft CS0436, CS0121 (twee `AddMediator`) en CS0122. Details in TAAK-195.

TAAK-147: een `internal` methode in een assembly met `InternalsVisibleTo` (behalve `DynamicProxyGenAssembly2`) telt ook als aanroepbaar van buiten → Manual. Sinds TAAK-148 alleen als een ander project de methode echt aanroept (melding noemt project en bestand:regel); anders Fixed (proof `edge-assemblies-friend`). `Mediator.SourceGenerator` komt alleen in projecten waarvan de originele code de echte MediatR-`AddMediatR` aanroept (semantisch, niet meer op naam; een eigen `AddMediator` telt niet). Een geswapte `global using Mediator;` kwalificeert eigen typen met een Mediator-naam in elk bestand van het project (proof `RequestHandler/edge-own-command-name-global`).

Regressie-edges bij REQ-1/REQ-2: `edge-assemblies` (`RegisterServicesFromAssemblies`), `edge-service-type-behavior` (`AddBehavior(typeof(IPipelineBehavior<,>), typeof(X<,>))`).

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

TAAK-063: `Mediator.SourceGenerator` alleen in projecten die `AddMediator`/`AddMediatR` aanroepen (`AddedPackage.OnlyWithCallTo`, syntactisch in `ProjectPackages`; sinds TAAK-147 semantisch, alleen MediatR's `AddMediatR`), met `PrivateAssets="all"` zodat hij niet transitief naar testprojecten stroomt (dat gaf de CS0436).

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
