# TAAK-183 — MediatR: `AddMediatR` over projectgrenzen (Mediator 3 `Assemblies`-optie onderzoeken)

**Status**: klaar (2026-10-12)
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)
**Doel**: de `mediatr.add-mediatr`-Manuals uit het corpus migreren automatisch, of blijven bewust Manual met onderbouwing in de patroondoc.

## Context voor de volgende agent

Varianten, nu Manual in `AddMediatRPattern.cs`:

- ardalis v9.1.0 `src/Clean.Architecture.Web/Program.cs:109`: scant `Clean.Architecture.Core`, handler `ContributorDeletedHandler` is `internal` (CS0122 vanuit Web). ardalis blijft daarnaast terecht Manual door Ardalis.SharedKernel.
- evo2 `Contracts/Fitnet.Contracts.Infrastructure/Mediation/MediationModule.cs:12`: scant `Contracts.Application`, handler `internal`; en `Common/Fitnet.Common.Infrastructure/Mediator/MediatorModule.cs:10`: `public` methode met `Assembly assembly`-parameter (handlers in de aanroeper).
- evo1 `Fitnet/Common/Events/EventBus/InMemory/InMemoryEventBusModule.cs:10`: `internal` methode, ook aangeroepen door `Fitnet.IntegrationTests` (InternalsVisibleTo) met `Assembly.GetExecutingAssembly()` om nep-consumers te registreren.

## Analyse

Mediator 3.x kan via `AddMediator(o => o.Assemblies = [...])` handlers uit gerefereerde assemblies genereren; internal handlers blijven onbereikbaar tenzij InternalsVisibleTo of generatie in het declarerende project. Eerst met een probe-project meten wat 3.0.2 kan (zoals bij TAAK-132), dan per variant kiezen.

## Resultaat (2026-10-12)

Probe (host + library, Mediator 3.0.2, net10.0):

| Opzet | Resultaat |
| --- | --- |
| internal handler, geen InternalsVisibleTo, `o.Assemblies = [typeof(Marker)]` | CS0122 in `Mediator.g.cs` |
| idem zonder `Assemblies` | CS0122 (de generator scant gerefereerde assemblies toch) |
| `InternalsVisibleTo("Host")`, met of zonder `Assemblies` | bouwt, beide handlers draaien |

`Assemblies` beperkt alleen de scan; het helpt niet tegen internal handlers. Beslissing per variant:

- Gescande assembly met internal handler die dit project als friend heeft → nu **Fixed** (`AddMediatRPattern.HiddenHandler` checkt `GivesAccessTo`).
- ardalis (`ContributorDeletedHandler` internal, geen friend; plus Ardalis.SharedKernel) → blijft Manual; melding noemt nu ook InternalsVisibleTo als uitweg.
- evo2 publieke methode met `Assembly`-parameter → blijft Manual (handlers bij de aanroeper; meerdere `AddMediator` geven één `IMediator`, TAAK-144).
- evo2 `MediationModule` (internal handlers, geen friend) → blijft Manual.
- evo1 internal methode die het testproject echt aanroept → blijft Manual (TAAK-148).

Gewijzigd: `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`, `src/Unshackle.Tests/Core/AddMediatRPatternTests.cs`
(theory `..._ManualWhenHandlerNotPublicAndNoFriend`: friend `Host` → Fixed, `Other` → Manual), `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`, `-verify.md`.
Geen proof: een proof is één bestand en kan geen tweede assembly met InternalsVisibleTo nabootsen; de unittest doet dat met twee compilaties.

## Stappen

1. [x] Probe: Mediator 3.0.2 `Assemblies` met public en internal handlers, en met InternalsVisibleTo.
2. [x] Beslissing per variant in `docs/projectinfo/patterns/` (add-mediatr).
3. [x] Proofs + implementatie voor de varianten die kunnen.
4. [x] Bewijs: Corpusrun 2026-10-12 op 5f9b366 (`CORPUSRUN-mediatr.md`, sectie Herhaling na TAAK-180..184): ardalis (1 add-mediatr + 11 SharedKernel), evo1 (1) en evo2 (2) houden precies de hierboven besloten Manuals; tests gelijk (9/9, 45/45, 26/52), 0 regressies.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
