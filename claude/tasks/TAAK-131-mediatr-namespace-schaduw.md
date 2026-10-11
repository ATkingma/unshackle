# TAAK-131 — MediatR: `using Mediator;` binnen een namespace wordt overschaduwd door eigen `*.Mediator`-namespace

**Status**: open
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` (evolutionary-architecture hfst. 2)

## Probleem

Fitnet heeft `namespace EvolutionaryArchitecture.Fitnet.Common.Infrastructure.Mediator` en zet usings binnen de
(file-scoped) namespace. Na de swap staat er `using Mediator;` in `...Common.Infrastructure.Events`; C# zoekt dat
eerst relatief en vindt de eigen namespace. Gevolg: CS0246 `INotification`/`IPublisher` niet gevonden.
Een projectnaam of map "Mediator" is in MediatR-projecten gewoon (`MediatorModule`, `Mediator/Behaviors`).

## Wat, waar, hoe

- **Waar**: `UsingDirectives` (swap van `RemovedNamespaces` → `AddedNamespaces`).
- **Hoe**: als `Mediator` vanuit de positie van de using naar iets anders dan de globale namespace bindt
  (`model.LookupNamespacesAndTypes` / bestaat er een `*.Mediator`-namespace in de compilatie), `using global::Mediator;` schrijven.
- Proof (pending): `src/Unshackle.Tests/Proofs/_Pending/MediatR/Usings/edge-namespace-shadow.*`.

## Stappen

- [ ] Detectie + `global::`
- [ ] Proof uit `_Pending` terugzetten (proof-runner moet een map voor de usings-swap accepteren), groen
- [ ] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
