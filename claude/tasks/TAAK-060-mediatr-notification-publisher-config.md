# TAAK-060 — mediatr: NotificationPublisher-instelling en dubbelzinnige using

**Status**: open
**Aangemaakt**: 2026-10-11
**Doel**: `cfg.NotificationPublisher = new TaskWhenAllPublisher()` / `new ForeachAwaitPublisher()` in `AddMediatR` → `cfg.NotificationPublisherType = typeof(...)` in `AddMediator`, en geen CS0104 meer na de fix.

## Context

- Gevonden in TAAK-037 (end-to-end op SampleShop): `samples/SampleShop/SampleShop/ServiceCollectionExtensions.cs`.
- Mediator 3.0.2 heeft `Mediator.TaskWhenAllPublisher` en `Mediator.ForeachAwaitPublisher`, en `MediatorOptions.NotificationPublisherType`.
- Na `fix` zet de swap `using MediatR;` → `using Mediator;` terwijl `using MediatR.NotificationPublishers;` blijft (Manual-bestand) → CS0104 op `TaskWhenAllPublisher`. Ook los van dit patroon: een swap mag geen dubbelzinnige naam achterlaten (bv. swap overslaan zolang een `MediatR.*`-using blijft).

## Stappen

- [ ] 1. `AddMediatRPattern`: de twee ingebouwde publishers mappen; `using MediatR.NotificationPublishers` laten weghalen (bv. `RemovedNamespaces` uitbreiden). Eigen publisher-klasse → blijft Manual (`mediatr.unsupported`).
- [ ] 2. Swap/using-logica in `Fixer`/`UsingDirectives`: geen CS0104 als er een `MediatR.*`-using achterblijft.
- [ ] 3. Proofs + end-to-end op een kopie van SampleShop.

## Zie ook

- `claude/tasks/archief/TAAK-037-mediatr-unsupported.md`
- `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`
