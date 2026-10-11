# TAAK-028 — mediatr.add-mediatr

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `services.AddMediatR(cfg => ...)` (MediatR 12.5.0) → `services.AddMediator(cfg => ...)` (Mediator 3.0.2).

## Context voor de volgende agent

- Rij 5 in `claude/tasks/mediatr-queue.md`. Voorbeeld: `samples/SampleShop/SampleShop/ServiceCollectionExtensions.cs`.
- `after` compileert alleen met de Mediator-source-generator; de proof-runner draait hem sinds TAAK-027.

## Analyse

- Mediator 3.0.2: `AddMediator(this IServiceCollection, Action<MediatorOptions>?)` en `MediatorOptions` worden door de source generator geschreven (namespace `Microsoft.Extensions.DependencyInjection` resp. `Mediator`). Opties: `Namespace`, `GenerateTypesAsInternal`, `NotificationPublisherType`, `ServiceLifetime` (standaard Singleton), `Assemblies`, `PipelineBehaviors`, `StreamPipelineBehaviors`. Gecontroleerd in de gegenereerde code van een scratch-project.
- De generator leest de toewijzingen uit de broncode (lifetime wordt letterlijk ingebakken); daarom alleen letterlijke `ServiceLifetime.X` en `typeof(...)`.
- MediatR 12.5 `MediatRServiceConfiguration`: `Lifetime` (standaard Transient), `AddOpenBehavior(Type, lifetime)`, `AddBehavior<S,I>(lifetime)`, `AddBehavior<I>(lifetime)`, `AddBehavior(Type, lifetime)`, `AddBehavior(Type, Type, lifetime)`, `RegisterServicesFrom*`.

## Keuzes

- **Altijd `ServiceLifetime` schrijven**, standaard `Transient`: MediatR registreert transient, Mediator singleton; zonder dit worden handlers met scoped afhankelijkheden captive.
- **Behavior met lifetime-argument → Manual**: Mediator heeft één lifetime voor alles.
- **`NotificationPublisher`, `RegisterGenericHandlers`, stream/pre/post → Manual** (hele aanroep): geen zekere 1-op-1-tegenhanger; publisher zou de using `MediatR.NotificationPublishers` achterlaten.
- **Lambda-parameternaam blijft** (`cfg`); inspringing van het eerste statement.
- De Fixer voegt `using Mediator;` toe als een bestand verandert, ook al is hij hier ongebruikt; onschuldig, staat zo in de proofs.

## Stappen

- [x] 1. Claim rij 5.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md` (approved).
- [x] 3. `AddMediatRPattern`, registratie in `MediatRFix`.
- [x] 4. Proofs `behaviors`, `expression`; Manual `unknown-call`, `behavior-lifetime`, `publisher`, `no-lambda`.
- [x] 5. Modellen, verify, README-rij, queue-rij.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 400/400, sample-tests 38/38.
- Scan SampleShop (`--fix mediatr`): add-mediatr 0/1 (Manual: `cfg.RegisterGenericHandlers = true`), notification-handler 3/0, pipeline-behavior 2/0, request-handler 5/0, void-request-handler 1/0, project-file 4/1 (MediatR blijft door de Manual). Totaal 15 fixed, 2 manual.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-027-mediatr-pipeline-behavior.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`
