# TAAK-154 — MediatR: `Task Handle(…) => X(…)` blokkeert de hele migratie

**Status**: open
**Aangemaakt**: 2026-10-11 (`claude/tasks/REGRESSIE-corpus-2.md`, klasse 3)

## Context

MODiX (MediatR 12.4.1): drie notification-handlers zijn niet-async met een expression body die een andere Task teruggeeft, bv. `public Task Handle(ReactionAddedNotificationV3 n, CancellationToken ct) => ReactionAddedAsync(n.Message, n.Channel, n.Reaction);` (`src/Modix.Bot/Responders/CommandErrors/CommandErrorResponder.cs:16,57`, `src/Modix.Bot/Responders/MutedUserJoinedResponder.cs:16`). `NotificationHandlerPattern` geeft Manual ("Task that is not Task.CompletedTask"), en door de alles-of-niets-regel worden ook de 20 andere findings niet herschreven: 0 bestanden gewijzigd.

Met die drie vooraf met de hand naar `async Task Handle(…) => await X(…);` gezet migreert de tool alles (23 Fixed), build groen, 477/477 tests (run `modix-b`).

## Stappen

1. [ ] Proof: notification- en request-handler met `Task`/`Task<T> Handle(…) => Expr;` en met `{ return Expr; }`.
2. [ ] Rewrite naar `async ValueTask Handle(…) => await Expr;` (resp. `ValueTask<T>`), of `new ValueTask(Expr)` zonder state machine. Let op `ConfigureAwait`-gebruik en exception-timing (synchroon gooien wordt bij `async` uitgesteld; bij `new ValueTask(Expr)` niet).
3. [ ] Bewijs: harnas op `src2/modix.tgz` (ongewijzigd) → 23 Fixed, 0 regressies.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/NotificationHandlerPattern.cs`
- `claude/tasks/mediatr-queue.md`
