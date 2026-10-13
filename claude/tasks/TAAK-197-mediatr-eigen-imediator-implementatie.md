# TAAK-197 — Eigen `IMediator`-implementatie in testcode (ardalis `NoOpMediator`)

**Status**: stap 1/4 — nog te starten
**Aangemaakt**: 2026-10-13 (uit de ardalis-run van TAAK-195)
**Doel**: ardalis v9.1.0 bouwt na `fix --fix mediatr` volledig, inclusief `tests/Clean.Architecture.UnitTests`.

## Context voor de volgende agent

- ardalis `github.com/ardalis/CleanArchitecture` tag v9.1.0: `fix --fix mediatr --force` → 31 Fixed, 0 Manual; Core/UseCases/Infrastructure/Web bouwen.
- `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` implementeert MediatR's `IMediator` zelf (`Task`-signaturen voor `Send`, `Publish`, `CreateStream`).
  Na de swap naar `Mediator.IMediator` geeft dat CS0535/CS0738: Mediator's interface heeft `ValueTask`-signaturen en andere overloads.
- Geen pattern vindt een klasse die `IMediator`/`ISender`/`IPublisher` implementeert.

## Nieuwe situatie (voorstel, eerst meten)

- Klasse die MediatR `IMediator`/`ISender`/`IPublisher` implementeert, met alleen triviale bodies (no-op, `Task.CompletedTask`, `default`) → members volgens Mediator 3.0.2's interface (`ValueTask<TResponse> Send<TResponse>(IRequest<TResponse>, ...)`, `Send(ICommand<…>)`, `Publish<TNotification>`, `CreateStream`, …).
- Anders Manual met reden ("eigen IMediator-implementatie: schrijf hem tegen Mediator's interface").

## Stappen

- [ ] 1. Mediator 3.0.2 `IMediator`/`ISender`/`IPublisher` members opsommen; ardalis `NoOpMediator` bekijken.
- [ ] 2. Proofs: no-op-implementatie (after), implementatie met logica (manual).
- [ ] 3. Pattern + docs (plan/modellen/verify, README-rij).
- [ ] 4. Bewijs: ardalis-clone bouwt en test na de fix.

## Zie ook

- `claude/tasks/archief/TAAK-195-mediatr-addmediatr-projectgrenzen.md`
- `claude/tasks/mediatr-queue.md`
