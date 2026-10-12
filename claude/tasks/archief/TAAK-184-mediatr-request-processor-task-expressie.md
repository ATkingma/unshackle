# TAAK-184 — MediatR: request-processor die een andere Task teruggeeft (`return Task.Run(...)`)

**Status**: ✅ voltooid — 2026-10-12
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`; actie 7 uit de run van 2026-10-11)
**Doel**: nadirbad VerticalSlice komt voorbij deze Manual (daarna blijft hij terecht Manual op de generieke `DomainEventNotification<T>`).

## Context voor de volgende agent

- nadir (`feb3a15e7ba03dc12404127e0a1f8f33b214783d`, MediatR 12.4.1): `src/Application/Common/Behaviours/LoggingBehaviour.cs:21 public Task Process(TRequest request, CancellationToken ct)` met `return Task.Run(...)`. `RequestProcessorPattern` eist `Task.CompletedTask` of `async`.
- Zelfde vorm als TAAK-154 (handlers): `return Expr;` → `return new ValueTask(Expr);` (synchrone exception-timing blijft gelijk).

## Stappen

1. [x] Proof `Proofs/MediatR/RequestProcessor/` met `return Task.Run(...)` en expression body.
2. [x] Rewrite in `RequestProcessorPattern.cs` naar `new ValueTask(Expr)`.
3. [x] (Corpusrun 2026-10-12 op 5f9b366 (`CORPUSRUN-mediatr.md`, sectie Herhaling na TAAK-180..184): bevestigd, enige root-Manual op nadir is de generieke notification; 30/30, 0 regressies) Bewijs: harnas op nadir → enige root-Manual is `mediatr.unsupported` (generieke notification).

## Resultaat

- `MediatRSyntax.WithCompletedValueTask`: elke andere teruggegeven `Task` wordt `new ValueTask(expr)` (alleen gebruikt door `RequestProcessorPattern`); Manual blijft voor niet-`Task`-types (ook `Task<T>`).
- Proofs: `RequestProcessor/other-return` (nu Fixed, was Manual), `RequestProcessor/task-run`. Patroon-docs (plan REQ-4, verify, modellen) en `proofs.md` bijgewerkt.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `claude/tasks/archief/TAAK-154-mediatr-handler-expression-body-task.md`
