# TAAK-181 — Projectbestand `<Project Sdk="..." />` (zelfsluitend) kan niet bewerkt worden

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)
**Doel**: een csproj die alleen uit `<Project Sdk="Microsoft.NET.Sdk" />` bestaat, blokkeert de projectbestand-stap niet meer.

## Context voor de volgende agent

- kgrzybek modular-monolith (`91c8ef24b4cb6ef558c95d8267fa07d68c7059f8`): `src/Modules/Meetings/Domain/CompanyName.MyMeetings.Modules.Meetings.Domain.csproj` is letterlijk `<Project Sdk="Microsoft.NET.Sdk" />` + CRLF.
- Melding "Could not edit the project files: ...Meetings.Domain.csproj cannot be edited without changing unrelated text" op **alle** projecten (210 cascade-Manuals). TAAK-134 (splicen) dekt dit geval dus niet.
- Waarschijnlijk hoeft de fix aan dit bestand niets te veranderen, maar wordt het toch gecontroleerd; of het zelfsluitende root-element wordt bij de roundtrip `<Project ...></Project>`.

## Stappen

1. [ ] DataAccess-test: roundtrip van `<Project Sdk="Microsoft.NET.Sdk" />` (CRLF, zonder slotnewline) laat de tekst gelijk.
2. [ ] Fix in `ProjectFile.cs`: ongewijzigde bestanden niet controleren/schrijven; zelfsluitend root-element splicen.
3. [ ] Bewijs: harnas op kgr (getrimde solution) → geen project-file-Manual; resterend alleen de 32 generieke decorators/`DomainNotificationBase<T>` (terecht Manual).

## Zie ook

- `src/Unshackle.DataAccess/Projects/ProjectFile.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
