# TAAK-181 — Projectbestand `<Project Sdk="..." />` (zelfsluitend) kan niet bewerkt worden

**Status**: ✅ voltooid — 2026-10-12
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)
**Doel**: een csproj die alleen uit `<Project Sdk="Microsoft.NET.Sdk" />` bestaat, blokkeert de projectbestand-stap niet meer.

## Context voor de volgende agent

- kgrzybek modular-monolith (`91c8ef24b4cb6ef558c95d8267fa07d68c7059f8`): `src/Modules/Meetings/Domain/CompanyName.MyMeetings.Modules.Meetings.Domain.csproj` is letterlijk `<Project Sdk="Microsoft.NET.Sdk" />` + CRLF.
- Melding "Could not edit the project files: ...Meetings.Domain.csproj cannot be edited without changing unrelated text" op **alle** projecten (210 cascade-Manuals). TAAK-134 (splicen) dekt dit geval dus niet.
- Waarschijnlijk hoeft de fix aan dit bestand niets te veranderen, maar wordt het toch gecontroleerd; of het zelfsluitende root-element wordt bij de roundtrip `<Project ...></Project>`.

## Uitkomst

- Oorzaak: het bestand zelf is niet het probleem (roundtrip is gelijk). De fout komt als een migratie een verwijzing aan zo'n leeg root-element toevoegt (de "transitief binnengekomen"-tak van `EditReferences`, bv. MediatR -> Mediator.Abstractions in een Domain-project): `RenderElement` gooide bij een zelfsluitend element met nieuwe kinderen.
- Fix: `ProjectFile.RenderElement` maakt van `<Project Sdk="..." />` + kinderen `<Project Sdk="...">...</Project>`; nieuwe regels in een leeg root volgen het regeleinde van het bestand (`ProjectFile.NewLine`, gebruikt in `AddNextTo`), dus CRLF blijft CRLF.
- Tests: `XmlProjectFileStoreTests` (`EditAsync_ProjectWithoutContent_...` met 4 vormen incl. CRLF, `EditAsync_SelfClosingProjectOfThisSolution_...`). Stap 3 (harnas op kgr) niet gedraaid: corpusproject niet beschikbaar in deze run; de unittests bootsen het geval na.
- Let op: `XmlProjectFileStoreTests` heeft op Windows al falende tests door `/repo`-paden tegenover `C:
epo` in de `Text`-helper; mijn tests gebruiken daarom `FullPathText`.

## Stappen

1. [x] DataAccess-test: roundtrip van `<Project Sdk="Microsoft.NET.Sdk" />` (CRLF, zonder slotnewline) laat de tekst gelijk.
2. [x] Fix in `ProjectFile.cs`: ongewijzigde bestanden niet controleren/schrijven; zelfsluitend root-element splicen.
3. [x] Bewijs: Corpusrun 2026-10-12 op 5f9b366 (`CORPUSRUN-mediatr.md`, sectie Herhaling na TAAK-180..184): kgr geen "cannot be edited"-Manual meer; resterend 32 generieke decorators/`DomainNotificationBase<T>` + 30 `unmatched-use` (Autofac-`MediatorModule` met `MediatR.Pipeline`-typen), terecht Manual. 246/287, 0 regressies.

## Gewijzigde bestanden

- `src/Unshackle.DataAccess/Projects/ProjectFile.cs`, `src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs`
- `src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs`

## Zie ook

- `src/Unshackle.DataAccess/Projects/ProjectFile.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
