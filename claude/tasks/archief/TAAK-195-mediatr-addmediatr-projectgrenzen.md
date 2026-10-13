# TAAK-195 — MediatR: `AddMediatR` over projectgrenzen 100% automatisch (internal handlers, `Assembly`-parameter, testproject)

**Status**: deels — internal handlers ✅ automatisch; `Assembly`-parameter (evo2) en testproject-aanroeper (evo1) ❌ mislukt (poging 1, geen exacte vertaling)
**Aangemaakt**: 2026-10-12 (100%-ronde, `claude/tasks/mediatr-queue.md` rij 25)
**Doel**: de `mediatr.add-mediatr`-Manuals van ardalis, evo1 en evo2 (TAAK-144, -148, -183) automatisch en exact migreren, of per variant meten waarom dat niet kan.

## Context voor de volgende agent

- Varianten (bronnen in `<probe-dir>/probe-195/` tijdens deze taak, daarna opgeruimd; opnieuw klonen: `github.com/ardalis/CleanArchitecture` tag v9.1.0, `github.com/evolutionary-architecture/evolutionary-architecture-by-example` met `core.longpaths`):
    - ardalis `src/Clean.Architecture.Web/Program.cs:109` scant Core (internal `ContributorDeletedHandler`) en UseCases.
    - evo2 `Contracts.Infrastructure/Mediation/MediationModule.cs:12` scant `Contracts.Application` (internal handlers); `Common.Infrastructure/Mediator/MediatorModule.cs:8` is een publieke `AddMediator(services, Assembly)` die Offers, Passes en `Common.IntegrationTests.WithFakeConsumers(executingAssembly)` aanroepen.
    - evo1 `Fitnet/Common/Events/EventBus/InMemory/InMemoryEventBusModule.cs:7` internal `AddInMemoryEventBus(services, assembly)`; `Fitnet.IntegrationTests/.../ConfigurationExtensions.cs:43` roept die in `ConfigureTestServices` aan met de testassembly om `TestEventConsumer : IIntegrationEventHandler<FakeEvent>` (beide in het testproject) te registreren.

## Probe (Mediator 3.0.2, net10.0; `<probe-dir>/probe-195/p1`)

| Opzet | Resultaat |
| --- | --- |
| Host (generator, `AddMediator`) + Tst (zonder generator) die `INotificationHandler<FakeEvent>` zelf in DI zet en `Publish(new FakeEvent())` doet | geen fout, handler **niet** aangeroepen: de gegenereerde `Publish` switcht op bij compile-time bekende typen, onbekend → stil genegeerd |
| Tst ook met generator en eigen `AddMediator` | CS0436 (`Mediator.Mediator`, `MediatorOptions`, `AssemblyReference` dubbel), CS0121 (twee `AddMediator`-extensies), CS0122 (internal host-handler) |
| Gegenereerde `AddMediator` | `IMediator`/`ISender`/`IPublisher` via `TryAdd`: de eerste aanroep wint; handlers via `Add` (tweede generator zou host-handlers dubbel registreren) |
| Internal handler in gerefereerd project + `[assembly: InternalsVisibleTo("Host")]` in bron | bouwt (TAAK-183) |

## Keuzes

1. **Internal handlers (ardalis, evo2 MediationModule) → automatisch.** Nieuw patroon `mediatr.internal-handler-access` (`InternalHandlerAccessPattern`): het eerste bestand (op pad) van het gescande project met een internal handler krijgt `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("<host>")]`. In het host-project is dat geen gedragsverschil (MediatR bereikte ze via reflectie). `AddMediatRPattern.RewriteAsync` is dan niet meer Manual (`CanGrantAccess`: C#-project van de solution, geen strong name). Gekozen boven csproj-`<InternalsVisibleTo>` (vraagt een nieuw `IProjectFileStore`-contract in DataAccess) en boven handlers `public` maken (CS0050/CS0051 als het requesttype internal is).
2. **Contract `ISolutionFinder`** (`Core/Abstractions`, één regel in `Fixer.ApplyPatternAsync`): `Find` zonder solution weet niet welk bestand een bevinding heeft; zonder dit kreeg elk bestand met een internal handler een lege Fixed-rij. Apart van `ISolutionPattern` zodat de vier AutoMapper-patterns (andere agents) niet hoeven te veranderen.
3. **evo1 → mislukt.** De nep-consumer en zijn event staan in het testproject. De host-generator kent `FakeEvent` niet, dus `Publish` doet niets (gemeten); een generator in het testproject bouwt niet (CS0436/CS0121/CS0122) en zou door `TryAdd` toch de host-`IMediator` laten winnen. Exact kan alleen als de testcode de DI-registratie van de host vervangt en de host-handlers niet dubbel registreert: geen veilige automatische vertaling.
4. **evo2 publieke `AddMediator(services, Assembly)` → mislukt.** Drie modules registreren elk hun assembly in één container; Mediator heeft één gegenereerde mediator per host. De enige exacte vorm is één `AddMediator` in de composition root (`Fitnet`) met `InternalsVisibleTo` van elke module, en de module-aanroepen weg. Maar de integratietests (`WithFakeConsumers(executingAssembly)`) lopen tegen hetzelfde probleem aan als evo1.
5. **Beste deelvertaling evo1/evo2**: blijft Manual (alles-of-niets); de Manual-tekst wijst de weg (één `AddMediator` in het host-project, testhandlers in de host of een eigen host per testproject). Met keuze 1 valt bij evo2 de `MediationModule`-Manual weg; de `MediatorModule`-Manual blijft.

## Gewijzigd

- `src/Unshackle.Core/Abstractions/ISolutionFinder.cs` (nieuw), `src/Unshackle.Core/Fixer.cs` (`FindAsync` als het pattern hem heeft)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs` (nieuw), `AddMediatRPattern.cs` (`ScannedAssemblies`, `IsHiddenHandler`, `CanGrantAccess`), `MediatRFix.cs` (pattern achteraan)
- `src/Unshackle.Tests/Helpers/TestProjects.cs` (meerprojectproofs: `// @project Naam -> Referenties`), `CombinedProofTests.cs`, `Core/InternalHandlerAccessPatternTests.cs`
- Proof `src/Unshackle.Tests/Proofs/MediatR/_Combined/add-mediatr-internal-handlers-other-project.{before,after}.cs` (ardalis-vorm: Core/UseCases/Web)
- Docs: `docs/projectinfo/patterns/mediatr-internal-handler-access-{plan,modellen,verify}.md`, `mediatr-add-mediatr-{plan,verify}.md`, README-rij, `proofs.md`

## Stappen

- [x] Probe: onbekende notification, tweede generator, `TryAdd`
- [x] Pattern + contract + proof + unittests
- [x] Docs (plan/modellen/verify, README, INDEX, proofs-doc)
- [x] ardalis-clone: `fix --fix mediatr --force` → 31 Fixed, 0 Manual (met vendored SharedKernel van een parallelle taak); Core/UseCases/Infrastructure/Web bouwen. Nieuw gat: `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` (eigen `IMediator`-implementatie, `Task`-signaturen) breekt de build (CS0535/CS0738) — TAAK-197.
- [!] evo1 en evo2-`MediatorModule`: geen exacte automatische vertaling (keuzes 3–4)

## Zie ook

- `claude/tasks/archief/TAAK-183-mediatr-addmediatr-over-assemblies.md`
- `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md`, `claude/tasks/archief/TAAK-148-mediatr-friend-echte-aanroep.md`
- `docs/projectinfo/patterns/mediatr-internal-handler-access-plan.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
