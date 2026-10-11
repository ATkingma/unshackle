# TAAK-132 — MediatR: abstracte generieke basisklasse onterecht Manual ("generic messages")

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` (kgrzybek modular-monolith: 10 van 12 `mediatr.unsupported`)

## Probleem

`UnsupportedPattern` (r. ~54) geeft Manual voor elk generiek type dat `IRequest<T>`/`INotification` implementeert,
ook `public abstract class CommandBase<TResult> : ICommand<TResult>`. De Mediator-generator registreert alleen
concrete typen: gecontroleerd met Mediator 3.0.2 (abstract `CommandBase<TResult> : IRequest<TResult>` +
`sealed Ping : CommandBase<int>` bouwt en `Send` geeft het juiste antwoord). De Manual blokkeert via alles-of-niets
de hele solution.

## Wat, waar, hoe

- **Waar**: `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`.
- **Hoe**: `symbol.IsAbstract` (en interfaces) uitzonderen; concrete generieke messages en generieke handlers blijven Manual.
  `derived-generic-message` (generiek concreet type onder niet-generieke basis) blijft Manual.
- Proof: `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/edge-abstract-generic-base.*` (swap-only, bouwt met de generator).

## Stappen

- [x] `IsAbstract`-uitzondering
- [x] Proof uit `_Pending` terugzetten, groen
- [x] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
