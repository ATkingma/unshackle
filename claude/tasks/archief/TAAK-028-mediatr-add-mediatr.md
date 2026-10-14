# TAAK-028 — mediatr.add-mediatr

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `services.AddMediatR(cfg => ...)` (MediatR 12.5.0) → `services.AddMediator(cfg => ...)` (Mediator 3.0.2).

## Context for the next agent

- Row 5 in `claude/tasks/mediatr-queue.md`. Example: `samples/SampleShop/SampleShop/ServiceCollectionExtensions.cs`.
- `after` only compiles with the Mediator source generator; the proof runner has been running it since TAAK-027.

## Analysis

- Mediator 3.0.2: `AddMediator(this IServiceCollection, Action<MediatorOptions>?)` and `MediatorOptions` are written by the source generator (namespace `Microsoft.Extensions.DependencyInjection` and `Mediator` respectively). Options: `Namespace`, `GenerateTypesAsInternal`, `NotificationPublisherType`, `ServiceLifetime` (default Singleton), `Assemblies`, `PipelineBehaviors`, `StreamPipelineBehaviors`. Checked in the generated code of a scratch project.
- The generator reads the assignments from the source code (the lifetime is baked in literally); therefore only literal `ServiceLifetime.X` and `typeof(...)`.
- MediatR 12.5 `MediatRServiceConfiguration`: `Lifetime` (default Transient), `AddOpenBehavior(Type, lifetime)`, `AddBehavior<S,I>(lifetime)`, `AddBehavior<I>(lifetime)`, `AddBehavior(Type, lifetime)`, `AddBehavior(Type, Type, lifetime)`, `RegisterServicesFrom*`.

## Choices

- **Always write `ServiceLifetime`**, default `Transient`: MediatR registers transient, Mediator singleton; without this, handlers with scoped dependencies become captive.
- **Behavior with a lifetime argument → Manual**: Mediator has one lifetime for everything.
- **`NotificationPublisher`, `RegisterGenericHandlers`, stream/pre/post → Manual** (whole call): no certain 1-to-1 counterpart; the publisher would leave the `MediatR.NotificationPublishers` using behind.
- **The lambda parameter name stays** (`cfg`); indentation of the first statement.
- The Fixer adds `using Mediator;` when a file changes, even though it is unused here; harmless, and that is how it is in the proofs.

## Steps

- [x] 1. Claim row 5.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md` (approved).
- [x] 3. `AddMediatRPattern`, registration in `MediatRFix`.
- [x] 4. Proofs `behaviors`, `expression`; Manual `unknown-call`, `behavior-lifetime`, `publisher`, `no-lambda`.
- [x] 5. Models, verify, README row, queue row.

## Result

- `dotnet build` 0 warnings, `dotnet test` 400/400, sample tests 38/38.
- Scan SampleShop (`--fix mediatr`): add-mediatr 0/1 (Manual: `cfg.RegisterGenericHandlers = true`), notification-handler 3/0, pipeline-behavior 2/0, request-handler 5/0, void-request-handler 1/0, project-file 4/1 (MediatR stays because of the Manual). Total 15 fixed, 2 manual.

## See also

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-027-mediatr-pipeline-behavior.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`
