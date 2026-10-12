# TAAK-183 — MediatR: `AddMediatR` over projectgrenzen (Mediator 3 `Assemblies`-optie onderzoeken)

**Status**: stap 1/4 — nog te starten
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)
**Doel**: de `mediatr.add-mediatr`-Manuals uit het corpus migreren automatisch, of blijven bewust Manual met onderbouwing in de patroondoc.

## Context voor de volgende agent

Varianten, nu Manual in `AddMediatRPattern.cs`:

- ardalis v9.1.0 `src/Clean.Architecture.Web/Program.cs:109`: scant `Clean.Architecture.Core`, handler `ContributorDeletedHandler` is `internal` (CS0122 vanuit Web). ardalis blijft daarnaast terecht Manual door Ardalis.SharedKernel.
- evo2 `Contracts/Fitnet.Contracts.Infrastructure/Mediation/MediationModule.cs:12`: scant `Contracts.Application`, handler `internal`; en `Common/Fitnet.Common.Infrastructure/Mediator/MediatorModule.cs:10`: `public` methode met `Assembly assembly`-parameter (handlers in de aanroeper).
- evo1 `Fitnet/Common/Events/EventBus/InMemory/InMemoryEventBusModule.cs:10`: `internal` methode, ook aangeroepen door `Fitnet.IntegrationTests` (InternalsVisibleTo) met `Assembly.GetExecutingAssembly()` om nep-consumers te registreren.

## Analyse

Mediator 3.x kan via `AddMediator(o => o.Assemblies = [...])` handlers uit gerefereerde assemblies genereren; internal handlers blijven onbereikbaar tenzij InternalsVisibleTo of generatie in het declarerende project. Eerst met een probe-project meten wat 3.0.2 kan (zoals bij TAAK-132), dan per variant kiezen.

## Stappen

1. [ ] Probe: Mediator 3.0.2 `Assemblies` met public en internal handlers, en met InternalsVisibleTo.
2. [ ] Beslissing per variant in `docs/projectinfo/patterns/` (add-mediatr).
3. [ ] Proofs + implementatie voor de varianten die kunnen.
4. [ ] Bewijs: harnas op evo1/evo2.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
