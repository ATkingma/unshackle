# TAAK-131 — MediatR: `using Mediator;` binnen een namespace wordt overschaduwd door eigen `*.Mediator`-namespace

**Status**: klaar (2026-10-11)
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

- [x] Detectie + `global::`
- [x] Proof uit `_Pending` terugzetten (proof-runner moet een map voor de usings-swap accepteren), groen
- [x] `dotnet build` + `dotnet test` groen

## Resultaat

- `UsingDirectives.IsShadowed`: bij de swap in place (`using MediatR;` → `Mediator`) binnen een namespace-declaratie wordt
  gekeken of een omvattende namespace (niet de globale) een lid met de eerste naamdeel heeft (`A.Infrastructure.Mediator`).
  Dan wordt het `using global::Mediator;`. Nieuwe usings (geen swap) vallen onder TAAK-115 (dan bovenaan het bestand).
- Proof verplaatst van `_Pending/MediatR/Usings/` naar `Proofs/MediatR/NotificationHandler/edge-namespace-shadow.*`
  (een handler erbij, zodat het patroon een bevinding geeft; een aparte Usings-map kent de proof-runner niet).
- Corpus: verse kopie evo2 (`scratchpad/v131/evo2`): 25 Fixed / 0 Manual, de CS0246's zijn weg. Build faalt nog op één
  IDE0060 (`MediatorModule.AddMediator(…, Assembly assembly)`: de parameter is na de AddMediatR-rewrite ongebruikt) — apart.


## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
