# TAAK-184 — MediatR: request-processor die een andere Task teruggeeft (`return Task.Run(...)`)

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`; actie 7 uit de run van 2026-10-11)
**Doel**: nadirbad VerticalSlice komt voorbij deze Manual (daarna blijft hij terecht Manual op de generieke `DomainEventNotification<T>`).

## Context voor de volgende agent

- nadir (`feb3a15e7ba03dc12404127e0a1f8f33b214783d`, MediatR 12.4.1): `src/Application/Common/Behaviours/LoggingBehaviour.cs:21 public Task Process(TRequest request, CancellationToken ct)` met `return Task.Run(...)`. `RequestProcessorPattern` eist `Task.CompletedTask` of `async`.
- Zelfde vorm als TAAK-154 (handlers): `return Expr;` → `return new ValueTask(Expr);` (synchrone exception-timing blijft gelijk).

## Stappen

1. [ ] Proof `Proofs/MediatR/RequestProcessor/` met `return Task.Run(...)` en expression body.
2. [ ] Rewrite in `RequestProcessorPattern.cs` naar `new ValueTask(Expr)`.
3. [ ] Bewijs: harnas op nadir → enige root-Manual is `mediatr.unsupported` (generieke notification).

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `claude/tasks/archief/TAAK-154-mediatr-handler-expression-body-task.md`
