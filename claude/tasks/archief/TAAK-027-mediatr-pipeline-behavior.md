# TAAK-027 — mediatr.pipeline-behavior

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `IPipelineBehavior<TReq,TRes>` (MediatR 12.5.0) → Mediator 3.0.2 `IPipelineBehavior<TMessage,TResponse>`.

## Context voor de volgende agent

- Rij 4 in `claude/tasks/mediatr-queue.md`. Hergebruikt `MediatRSyntax` (TAAK-025).
- Voorbeelden: `samples/SampleShop/SampleShop/Behaviors/LoggingBehavior.cs` (open, async), `ValidationBehavior.cs` (gesloten, `return next()`).

## Analyse

Signaturen gecontroleerd met reflectie op de echte assemblies (scratch-project met Mediator 3.0.2):

- Mediator: `ValueTask<TResponse> Handle(TMessage, MessageHandlerDelegate<TMessage,TResponse>, CancellationToken)`, `TMessage : IMessage`; `MessageHandlerDelegate.Invoke(TMessage, CancellationToken)`.
- MediatR 12.5: `Task<TResponse> Handle(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)`; `next()` en `next(token)` compileren beide (proof `next-token`).

| Geval                                                   | Herschrijving                                                      |
| ------------------------------------------------------- | ------------------------------------------------------------------ |
| open behavior (request = typeparameter)                 | + `IMessage` in de constraint (voor `new()`), of nieuwe `where`    |
| gesloten behavior                                       | geen constraint: het request-type wordt via de swap een `IMessage` |
| `next()` / `next(token)`                                | `next(request, token)` met de parameternamen van `Handle`          |
| niet-async, returns `next(...)`/`Task.FromResult`       | `ValueTask.FromResult`; `next(...)` geeft al een `ValueTask`       |
| `next` opgeslagen/doorgegeven, andere return, expliciet | Manual                                                             |

## Keuzes

- **Pattern-node is het type, niet de methode**: de `IMessage`-constraint staat op de klasse. `FindHandleMethods` gebruikt nu `DescendantNodesAndSelf`, zodat hij ook met het type als root werkt (zelfde resultaat voor een compilation unit).
- **`IPipelineBehavior\`2`in`SwappedTypes`**: anders houdt de basislijst `using MediatR`vast. Het pattern dekt het signatuurverschil; bij Manual houdt`RequestHandlerDelegate`de MediatR-using en het pakket vast (zelfde gedrag als de handler-patronen). Test`SwappedTypes_PipelineBehavior_IsNotSwapped...` verwijderd.
- **Proof-runner draait de Mediator-source-generator** op elke MediatR-`after` (`ProofFix.AfterGenerators`, `TestReferences.MediatorGenerators`, `TestSolution.CompileErrors(..., generators)`). Het pakket `Mediator.SourceGenerator` staat in het testproject met `IncludeAssets="none"` (geen analyzer van het testproject zelf); de DLL's worden naar `Generators/` in de output gekopieerd en via `AnalyzerFileReference` + `AnalyzerLoader` geladen. Nodig voor rij 5 (`AddMediator` bestaat alleen door de generator), en het maakt alle MediatR-proofs strenger. `Microsoft.Extensions.DependencyInjection.Abstractions` zit nu in de MediatR- en Mediator-referenties.

## Stappen

- [x] 1. Claim rij 4.
- [x] 2. Plan `docs/projectinfo/patterns/mediatr-pipeline-behavior-plan.md` (approved).
- [x] 3. `PipelineBehaviorPattern`, registratie + swap in `MediatRFix`.
- [x] 4. Proofs `open-async`, `no-constraint`, `new-constraint`, `closed`, `next-token`; Manual `next-stored`, `next-passed`, `explicit`.
- [x] 5. Modellen, verify, README-rij, queue-rij.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 400/400, sample-tests 38/38.
- Scan SampleShop: `mediatr.pipeline-behavior` 2 Fixed, 0 Manual.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-028-mediatr-add-mediatr.md`
- `docs/projectinfo/patterns/mediatr-pipeline-behavior-plan.md`
