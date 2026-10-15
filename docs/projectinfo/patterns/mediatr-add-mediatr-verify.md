# Verification: mediatr-add-mediatr

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`](mediatr-add-mediatr-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                    |

> Verified by an AI agent against the code at uncommitted changes (basis b53e3cc).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`, registration in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/AddMediatR/`.

| REQ   | Verdict | Evidence                            |
| ----- | ------- | ----------------------------------- |
| REQ-1 | Met | `behaviors`                         |
| REQ-2 | Met | `expression`                        |
| REQ-2b | Met | `expression-several`               |
| REQ-3 | Met | `unknown-call`, `behavior-lifetime` |
| REQ-4 | Met | own publisher (no proof; `publisher` is Fixed since TAAK-060) |
| REQ-5 | Met | `no-lambda`                         |
| REQ-6 | Met | `generic-handlers` (TAAK-037)       |
| REQ-7 | Met | `MediatorHostsTests` (AddMediatR host next to host; scan of other host → Manual; TAAK-213) |
| Extension TAAK-060 | Met | `publisher`, `edge-publisher-qualified` |
| Extension TAAK-061/062 | Met | `processors`, `empty` |
| Extension TAAK-144 | Met | `edge-assemblies-public`, `AddMediatRPatternTests` |
| Extension TAAK-183 | Met | `AddMediatRPatternTests.Rewrite_ScannedAssemblyOfOtherProject_ManualWhenHandlerNotPublicAndNoFriend` |
| Extension TAAK-147 | Met | `edge-assemblies-friend`, `AddMediatRPatternTests`, `SwappedTypesTests` |
| Extension TAAK-195 | Met | `_Combined/add-mediatr-internal-handlers-other-project`, `InternalHandlerAccessPatternTests` |
| Extension TAAK-240 | Met | `AddMediatRPatternTests.RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace`; scan kgrzybek |
| Extension TAAK-148 | Met | `edge-assemblies-friend` (now Fixed), `AddMediatRPatternTests.RewriteAsync_InternalMethodWithFriend_ManualOnlyWhenOtherProjectCallsIt` |

TAAK-117: a parameter that only went to a dropped `RegisterServicesFrom...` (IDE0060) stays. TAAK-141: if the call is a standalone statement, `_ = parameter;` is put before it (no IDE0060, signature and callers unchanged; proof `edge-assemblies`, evo1 builds again). Otherwise `Fixed` with a warning in the report (`AddMediatRPatternTests`).

TAAK-144: if that method is public (evo2's shared `MediatorModule.AddMediator(services, assembly)`), or the call scans another project with an internal handler, then Manual; proof `edge-assemblies` is therefore now `internal`. In addition, an own type named like a Mediator type (`ICommand`) gets the fully qualified name where `using Mediator;` comes in (`IFix.AddedTypes`, proof `RequestHandler/edge-own-command-name`). Harness: evo2 `ok` (Manual, unchanged, 39/39), evo1 `ok` (45/45).

TAAK-183: probe with Mediator 3.0.2 (host + library with public and internal handler): without `InternalsVisibleTo` CS0122 in `Mediator.g.cs`, with or without `o.Assemblies = [typeof(Marker)]`; with `InternalsVisibleTo("Host")` it builds and runs (both handlers). An internal handler in an assembly that has this project as a friend is therefore no longer Manual. ardalis (no friend, plus Ardalis.SharedKernel), evo1 (test project as caller) and evo2 (public method with `Assembly` parameter, internal handlers without friend) rightly stay Manual.

TAAK-195: an internal handler in a project of the solution is no longer Manual; `mediatr.internal-handler-access` adds `InternalsVisibleTo` (proof `_Combined/add-mediatr-internal-handlers-other-project`; ardalis clone: 0 Manual, Web builds). evo1 and evo2 stay Manual; measured (Mediator 3.0.2): a notification unknown to the host generator is silently ignored by `Publish` (even if the handler is in DI), and a second generator in a test project gives CS0436, CS0121 (two `AddMediator`) and CS0122. Details in TAAK-195.

TAAK-147: an `internal` method in an assembly with `InternalsVisibleTo` (except `DynamicProxyGenAssembly2`) also counts as callable from outside → Manual. Since TAAK-148 only if another project actually calls the method (the message names project and file:line); otherwise Fixed (proof `edge-assemblies-friend`). `Mediator.SourceGenerator` only goes into projects whose original code calls the real MediatR `AddMediatR` (semantically, no longer by name; an own `AddMediator` does not count). A swapped `global using Mediator;` qualifies own types with a Mediator name in every file of the project (proof `RequestHandler/edge-own-command-name-global`).

Regression edges for REQ-1/REQ-2: `edge-assemblies` (`RegisterServicesFromAssemblies`), `edge-service-type-behavior` (`AddBehavior(typeof(IPipelineBehavior<,>), typeof(X<,>))`).

`after` only compiles with the Mediator source generator (which writes `AddMediator`); the proof runner runs it (`ProofFix.AfterGenerators`). The Fixer adds `using Mediator;`, even though it is unused in `behaviors`/`expression` (harmless).

Scan on SampleShop (after `dotnet restore`): `mediatr.add-mediatr` 0 Fixed, 1 Manual: `'cfg.RegisterGenericHandlers = true' has no direct Mediator equivalent` (`ServiceCollectionExtensions.cs:20`; after that also stream behavior, pre/post-processors and publisher, which are all Manual).

After TAAK-037 `RegisterGenericHandlers` is dropped; the Manual on SampleShop is now `cfg.NotificationPublisher = new TaskWhenAllPublisher()` (and after that stream behavior and pre/post-processors).

## Deviations

None.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 400/400 green                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green                             |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## Follow-up (2026-10-11)

TAAK-060: `cfg.NotificationPublisher = new TaskWhenAllPublisher()` / `new ForeachAwaitPublisher()` (also MediatR-qualified) → `cfg.NotificationPublisherType = typeof(global::Mediator.X)` (Mediator 3.0.2: `Mediator.TaskWhenAllPublisher`, `Mediator.ForeachAwaitPublisher`, `MediatorOptions.NotificationPublisherType`). `global::` does not bind to MediatR during the fix, so `using MediatR.NotificationPublishers;` (now in `RemovedNamespaces`) disappears. If one removed using remains (e.g. AddMediatR Manual), they all remain (`UsingDirectives.UsedRemovedNamespaces`): no CS0104/CS0029. Proofs `publisher` (was manual) and `edge-publisher-qualified`. An own publisher stays Manual (`mediatr.unsupported`).

TAAK-063: `Mediator.SourceGenerator` only in projects that call `AddMediator`/`AddMediatR` (`AddedPackage.OnlyWithCallTo`, syntactically in `ProjectPackages`; since TAAK-147 semantically, only MediatR's `AddMediatR`), with `PrivateAssets="all"` so it does not flow transitively to test projects (that caused the CS0436).

End-to-end (`fix --fix mediatr` on a copy of SampleShop, after restore): 20 Fixed, 8 Manual; build without CS0436 (only MSG0003/MSG0005 from the generic `CountQuery`). Manual: pre/post-processor, exception handler, stream behavior (TAAK-061/062), generic `CountQuery`, and as a result `AddMediatR` (`AddOpenStreamBehavior`). Because the registration stays MediatR, 28/38 sample tests fail at startup (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); that is resolved with TAAK-061/062.

## Updated (TAAK-061/062, review B M3/M4, 2026-10-11)

- `AddOpenRequestPreProcessor`/`AddRequestPreProcessor` → `PipelineBehaviors` before the behaviors (open: `typeof(X<>)` → `typeof(X<,>)`), post-processors after; closed exception handlers and processors from the own project (MediatR assembly scan) first. `AddOpenStreamBehavior`/`AddStreamBehavior` → `StreamPipelineBehaviors`. Proof `processors`.
- M3: new statements only get the indentation; comments of all old statements come before the first (proof `processors`).
- M4: empty block `cfg => { }` → `cfg => cfg.ServiceLifetime = ServiceLifetime.Transient` (proof `empty`).
- `Find` also accepts a candidate symbol: after another file has already been rewritten, the lambda no longer binds cleanly (found with the SampleShop integration test).
- `unknown-call` now uses `AddOpenBehaviors(...)` (the pre-processor is converted).

## See also

- [mediatr-add-mediatr-plan.md](mediatr-add-mediatr-plan.md)
- [mediatr-add-mediatr-modellen.md](mediatr-add-mediatr-modellen.md)
