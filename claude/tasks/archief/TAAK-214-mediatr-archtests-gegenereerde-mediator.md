# TAAK-214 — MediatR: gegenereerde Mediator-typen breken kgrzybek-archtests (module-afhankelijkheid)

**Status**: ✅ voltooid — legitiem gedragsverschil, geen codewijziging (beslissing gedocumenteerd)
**Afgerond**: 2026-10-13
**Aangemaakt**: 2026-10-13 (uit TAAK-213, corpus-ronde 6)
**Doel**: kgrzybek `91c8ef2` na `fix --fix mediatr`: tests 246/287 → 246/287 (nu 242/287).

## Context voor de volgende agent

- `_results/r6/kgr/compare.json`: build ok, 4 regressies, alle in `CompanyName.MyMeetings.ArchTests.Modules.ModuleTests`:
  `{Administration,Meetings,Payments,UserAccess}Module_DoesNotHave_Dependency_On_Other_Modules`, melding
  `But was: < <Mediator.Mediator>, <Mediator.Internals.ContainerMetadata> >`.
- De test (`src/Tests/ArchTests/Modules/ModuleTests.cs`) neemt alle typen van de module-assemblies (behalve `INotificationHandler<>`-implementaties
  en `*IntegrationEventHandler`) en eist geen afhankelijkheid op de namespaces van andere modules.
- Vermoeden (niet geverifieerd): de generator van elke module-host scant alle verwezen assemblies, ook IntegrationEvents-contracten van andere
  modules, en noemt die messages in de gegenereerde `Mediator`/`ContainerMetadata`. Onder MediatR bestond zo'n type niet.
- Registrations staat niet in de lijst: daar beperkt TAAK-213 al `Assemblies`.

## Stappen

- [x] 1. Gegenereerde `Mediator.g.cs` van Administration.Infrastructure bekeken (`EmitCompilerGeneratedFiles`, r6-werkboom).
- [x] 2. Optie `Assemblies` beperken uitgeprobeerd: verworpen (zie Probe).
- [x] 3. Beslissing: legitiem verschil; bewijs met een aangepaste archtest in een kopie (geen r7 nodig: geen codewijziging).

## Probe (r6-werkboom kgrzybek, Administration.Infrastructure)

- Vermoeden **bevestigd**. De gegenereerde code noemt typen uit andere modules: 43× `Modules.Meetings.IntegrationEvents`
  (o.a. `MeetingGroupProposedIntegrationEvent`, `MemberCreatedIntegrationEvent`) en 15× `Modules.Registrations.IntegrationEvents`
  (`NewUserRegisteredIntegrationEvent`). Daarnaast eigen Application/Domain/Infrastructure, BuildingBlocks.
- Die events hebben handlers in Administration.Application (`MeetingGroupProposedIntegrationEventHandler`, `NewUserRegisteredIntegrationEventHandler`):
  de generator moet ze noemen om ze te kunnen dispatchen (`Publish`-switch, `ContainerMetadata`-wrappers).
- Met `Assemblies = [MediatorModule, Administration.Application]` (handmatig, daarna teruggezet): build ok en geen vreemde typen meer, maar ook
  de domain events en integration events verdwijnen uit de `Publish`-switch. De `default`-tak doet dan **niets** (`ThrowInvalidNotification`
  gooit alleen bij een niet-`INotification`): die handlers zouden stil niet meer draaien. Onder MediatR draaiden ze wel. **Verworpen.**
- `GenerateTypesAsInternal` of `Namespace` helpen niet: NetArchTest bekijkt ook internal typen; het type blijft in de module-assembly.

## Beslissing

Legitiem gedragsverschil, geen fout in de migratie. De archtest kijkt naar alle typen in de module-assemblies en zondert
`INotificationHandler<>`-implementaties al uit, juist omdat die van andere modules mogen afhangen. De door Mediator gegenereerde dispatcher
(`Mediator.Mediator`, `Mediator.Internals.ContainerMetadata`) is hetzelfde soort afhankelijkheid, maar dan in gegenereerde code.
Bewijs: met `.And().DoNotResideInNamespace("Mediator")` in de 4 tests (kopie in r6, teruggezet) slaagt `CompanyName.MyMeetings.ArchTests`
8/8; daarmee is kgrzybek 246/287, gelijk aan de basis.
Advies voor gebruikers: archtests die alle typen van een assembly bekijken, moeten de namespace `Mediator` uitzonderen.
Unshackle meldt dit niet automatisch: zo'n waarschuwing is niet betrouwbaar af te leiden (welke test wat bekijkt staat niet in de code van de host).
Update: sinds TAAK-240 geeft de fix wel een algemene waarschuwing zodra de solution NetArchTest/ArchUnitNET refereert.

## Zie ook

- `claude/tasks/archief/TAAK-213-mediatr-host-verwijst-naar-host.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 6"
- `claude/tasks/mediatr-queue.md`
