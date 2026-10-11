# TAAK-061 — mediatr: pre/post-processors en exception-handlers herschrijven

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Doel**: in plaats van Manual (`mediatr.unsupported`) echte patronen voor `IRequestPreProcessor`, `IRequestPostProcessor` en `IRequestExceptionHandler`, plus de bijbehorende `AddMediatR`-aanroepen.

## Context

- Gevonden in TAAK-037. Met de hand bewezen op een kopie van SampleShop (38/38 groen):
    - `class AuditPreProcessor<TRequest> : IRequestPreProcessor<TRequest>` + `Task Process(request, ct)` → `class AuditPreProcessor<TMessage, TResponse> : MessagePreProcessor<TMessage, TResponse> where TMessage : IMessage` + `protected override ValueTask Handle(message, ct)`. Let op: arity verandert (`<>` → `<,>`).
    - Post-processor idem met `Handle(message, response, ct)`.
    - `IRequestExceptionHandler<TReq, TRes, TEx>` + `state.SetHandled(x); return Task.CompletedTask;` → `MessageExceptionHandler<TReq, TRes, TEx>` + `protected override ValueTask<ExceptionHandlingResult<TRes>> Handle(request, exception, ct)` met `return Handled(x);` (anders `NotHandled`).
    - `cfg.AddOpenRequestPreProcessor(typeof(X<>))`/`AddOpenRequestPostProcessor(typeof(Y<,>))` → in `PipelineBehaviors` (pre vóór de behaviors, post erna); exception-handlers ook in `PipelineBehaviors`.

## Stappen

- [x] 1. Patronen + proofs (simpel, async, `SetHandled`-varianten, Manual voor afwijkende vormen).
- [x] 2. `AddMediatRPattern`: processor-aanroepen mappen (arity aanpassen) en geregistreerde exception-handlers toevoegen.
- [x] 3. Proof-docs, end-to-end op SampleShop.

## Resultaat

- Patronen + proofs, `AddMediatRPattern` uitgebreid, end-to-end via `SampleShopTests.FixMediatRTwice_...` (build + 38 tests groen na migratie).
- Nieuw: alles-of-niets (`IFix.AllOrNothing`, `MediatRFix` = true): één Manual → solution en projectbestanden onaangeraakt, Fixed-bevindingen gemeld als Manual "Not rewritten".

## Zie ook

- `claude/tasks/archief/TAAK-037-mediatr-unsupported.md`
- `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`
