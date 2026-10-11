# TAAK-135 — Laden: Razor-componenten, `.dcproj` en niet-ladende bijprojecten blokkeren de hele solution

**Status**: open
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` (eShopOnWeb, dotnet/eShop, kgrzybek)

## Probleem

`MsBuildSolutionStore.LoadAsync` weigert de solution bij:

1. **CS0246/CS0234 op Razor-gegenereerde typen**: eShopOnWeb `BlazorAdmin` (`Edit`, `Delete`, `Details` zijn
   `.razor`-componenten), dotnet/eShop `WebApp` (`eShop.WebApp.Components`). Bouwt prima met `dotnet build`;
   in de workspace draait de Razor-generator kennelijk niet. Zelfs na restore: "Types do not resolve".
2. **Niet-C#-projecten**: `docker-compose.dcproj` ("extension not associated with a language").
3. **Eén bijproject dat niet laadt**: kgrzybek `_build` (Nuke) en `Database.Build` (MSBuild.Sdk.SqlProj) — ze
   gebruiken geen MediatR maar houden de hele migratie tegen.

Twee van de bekendste MediatR-voorbeelden (eShopOnWeb, eShop) laden daardoor nooit.

## Wat, waar, hoe

- **Waar**: `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs` (`IsTolerable`, de CS0246-check).
- **Hoe**: (1) CS0246-check alleen op projecten die het pakket van de fix refereren, of diagnostics in
  `*.razor`/gegenereerde bestanden en onbekende namen die als `.razor`-bestand in het project bestaan negeren;
  (2) projecten zonder taal (`.dcproj`, `.sqlproj`) tolereren als waarschuwing; (3) een niet-ladend project dat
  het te verwijderen pakket niet refereert tolereren. Waarschuwing in het rapport houden.
- Geen proof: unittests in `src/Unshackle.Tests/DataAccess/`.

## Stappen

- [ ] Rode tests (razor-component, dcproj, project zonder pakket dat faalt)
- [ ] Tolerantie
- [ ] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
