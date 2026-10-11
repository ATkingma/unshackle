# TAAK-080 — mediatr: handler via eigen marker-interface wordt niet gevonden

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: corpus-onderzoek (`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`)

## Probleem

Projecten (kgrzybek/modular-monolith-with-ddd, evolutionary-architecture-by-example) definiëren eigen interfaces:
`IAppQueryHandler<TQ,TR> : IRequestHandler<TQ,TR>` en `IAppEventHandler<T> : INotificationHandler<T>`.
Handlers implementeren alleen die eigen interface. `mediatr.request-handler` en `mediatr.notification-handler` vinden niets
("the pattern found nothing in the proof"), dus `Task Handle` blijft staan en de code compileert niet tegen Mediator.

## Wat, waar, hoe

- **Waar**: de interface-herkenning in `src/Unshackle.Core/Fixes/MediatR/MediatRSyntax.cs` / de handler-patronen.
- **Hoe**: kijk naar `AllInterfaces` van het symbool in plaats van alleen de directe base-list; zoek de `Handle`-implementatie via `FindImplementationForInterfaceMember`.
- Waarschijnlijk geldt hetzelfde voor `mediatr.void-request-handler` (`ICommandHandler<T> : IRequestHandler<T>`): proof toevoegen.

## Stappen

- [x] Proofs uit `src/Unshackle.Tests/Proofs/_Pending/MediatR/{RequestHandler,NotificationHandler}/edge-marker-interface.*` groen maken en terugzetten
- [x] Void-variant proof
- [x] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/mediatr-queue.md` (rij 9)
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md`
