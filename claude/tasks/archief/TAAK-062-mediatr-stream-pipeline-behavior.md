# TAAK-062 — mediatr: stream-pipeline-behavior herschrijven

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Doel**: `IStreamPipelineBehavior<TRequest, TResponse>` herschrijven in plaats van Manual, en `cfg.AddOpenStreamBehavior(typeof(X<,>))` → `cfg.StreamPipelineBehaviors = [typeof(X<,>)]`.

## Context

- Gevonden in TAAK-037; met de hand bewezen op een kopie van SampleShop (`Behaviors/StreamLoggingBehavior.cs`):
  `where TRequest : IStreamRequest<TResponse>` → `where TRequest : IStreamMessage`; `StreamHandlerDelegate<TResponse> next` → `StreamHandlerDelegate<TRequest, TResponse> next`; `next()` → `next(request, cancellationToken)`. Returntype `IAsyncEnumerable<TResponse>` blijft.
- Zelfde aanpak als `PipelineBehaviorPattern` (TAAK-027); `IStreamPipelineBehavior`2`kan dan naar`SwappedTypes`.

## Stappen

- [x] 1. Patroon + proofs (`next` doorgegeven/opgeslagen → Manual, zoals bij pipeline-behavior).
- [x] 2. `AddMediatRPattern`: `AddOpenStreamBehavior`/`AddStreamBehavior` → `StreamPipelineBehaviors`.
- [x] 3. Docs, end-to-end op SampleShop.

## Resultaat

- Patronen + proofs, `AddMediatRPattern` uitgebreid, end-to-end via `SampleShopTests.FixMediatRTwice_...` (build + 38 tests groen na migratie).
- Nieuw: alles-of-niets (`IFix.AllOrNothing`, `MediatRFix` = true): één Manual → solution en projectbestanden onaangeraakt, Fixed-bevindingen gemeld als Manual "Not rewritten".

## Zie ook

- `claude/tasks/archief/TAAK-027-mediatr-pipeline-behavior.md`
- `claude/tasks/archief/TAAK-037-mediatr-unsupported.md`
