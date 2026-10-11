# TAAK-130 — MediatR: handler via marker-interface in ander bestand wordt gemist (bestandsvolgorde)

**Status**: klaar
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` (evolutionary-architecture hfst. 1 en 2)

## Probleem

`Fixer` draait de patterns bestand voor bestand op de **al bijgewerkte** solution. Staat de marker
(`interface IIntegrationEventHandler<in T> : INotificationHandler<T>`) in een bestand dat eerder aan de beurt is
(`Common/...` vóór `Offers/...`), dan is zijn `using MediatR` al `using Mediator`. Het semantisch model van het
handlerbestand ziet dan `Mediator.INotificationHandler`, `MediatRSyntax.FindHandleMethods` vindt niets, er komt
**geen Fixed en geen Manual**. Gevolg: alles-of-niets laat het door, de build breekt (CS0738 `Task Handle`).

Gereproduceerd: twee bestanden `A.cs` (marker) + `B.cs` (handler) → handler gemist; `A.cs` hernoemd naar `Z.cs` → gevonden.
De bestaande proof `MediatR/NotificationHandler/edge-marker-interface` staat in één bestand en ziet dit niet.

## Wat, waar, hoe

- **Waar**: `src/Unshackle.Core/Fixer.cs` (lus over `files`, r. ~64–80).
- **Hoe**: `Find` en de semantische vragen op het model van de **oorspronkelijke** solution laten draaien
  (`original`), alleen herschrijven op de lopende root; of eerst alle findings verzamelen, dan pas toepassen.
  Raakt elke cross-file symboolcheck (marker-interfaces, behavior-constraints, basisklassen), ook bij FA.
- Proof kan niet (proofs zijn één bestand): unittest in `src/Unshackle.Tests/Core/` met twee documenten, marker
  in het bestand dat alfabetisch eerst komt.

## Stappen

- [ ] Rode test: twee documenten, handler-bestand na marker-bestand
- [ ] Fixer op originele semantiek
- [ ] `dotnet build` + `dotnet test` groen

## Uitkomst (2026-10-11)

**Status**: klaar. `Fixer` fixt elk bestand in een werkkopie van de **originele** solution (alleen dat bestand verandert)
en zet de eindtekst daarna in de verzamelde solution. Test `FixerTests.FixAsync_MarkerInterfaceInOtherFile_FindsHandlerWhateverTheFileOrder`
(marker voor/na het handler-bestand, echte `MediatRFix`); was rood bij marker-eerst. Proof-runner hoefde niet aangepast.

Bewijs: evo1 CS0738 weg (build nog rood op MSG0005 = TAAK-136 en IDE0005/IDE0065/IDE0060 door usings-plaatsing in
`InMemoryEventBusModule.cs`, `PassExpiredEventHandler.cs`, `RegisterEndpoint.cs` - nieuw, nog geen taak). evo2: 25 Fixed (sim: 22,
de gemiste handlers worden nu gevonden); build alleen nog rood op TAAK-131 (CS0246/CS1961 door `*.Mediator`-schaduw).

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/archief/TAAK-080-mediatr-marker-interface-handler.md`
