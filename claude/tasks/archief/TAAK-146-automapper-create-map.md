# TAAK-146 — AutoMapper-infrastructuur en automapper.create-map

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: rij 0 (`AutoMapperFix`, registratie, proof-runner) en rij 1 (`automapper.create-map`) van `claude/tasks/automapper-queue.md`.

## Context voor de volgende agent

- Fix: `src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs`, geregistreerd in `src/Unshackle.Interface/Cli.cs` (composition root, ook `list`).
- Proof-runner: map `AutoMapper` in `ProofTests.s_fixes`, `TestReferences.AutoMapper` (AutoMapper 14.0.0 + DI-abstracties) voor before én after: een (leeg) profile blijft AutoMapper gebruiken.
- Rijen 2 en 3 (`ForMember` Ignore/MapFrom) breiden `CreateMapPattern.Problem`/de statement-check uit; ze hoeven geen eigen profile-herkenning.

## Keuzes

- **Hele profile of niets**: `Find` geeft de profile-klasse, niet elk statement. Een extensiemethode moet in een top-level statische klasse; alleen een vervanging van de klasse door `[profile, <Profile>Extensions]` kan die toevoegen. Eén niet-ondersteunde map → Manual voor het hele profile.
- **`AllOrNothing = true` en `Map`/`ProjectTo` → Manual**: een verwijderde `CreateMap` terwijl `mapper.Map` nog draait faalt pas tijdens runtime (missing type map). Tot rij 4 er is, wijzigt de fix dus niets in een solution die `Map` aanroept. Rij 4 haalt die Manual weg.
- **Alleen exact gelijke naam en type**, geen collecties: AutoMapper matcht hoofdletterongevoelig, flattent, roept `GetX()` aan, converteert typen en kopieert collecties. Al die gevallen → Manual met reden.
- **Parameter `source`** in plaats van `a`: een typenaam als `Event` geeft anders een keyword.
- **`public`/`internal`** volgt de echte toegankelijkheid van A en B (anders CS0051).
- **Leeg profile blijft staan, geen `Package`-migratie**: verwijderen breekt `typeof(XProfile)`/`AddProfile<X>` in andere projecten. Het pakket blijft dus nodig; `IFix.Package` komt pas met de profile-, map- en add-automapper-rijen.
- **Tweede run idempotent**: een geleegd profile wordt niet meer gevonden (anders zou een Manual via AllOrNothing de eerste run ongedaan maken).

## Stappen

- [x] 1. `AutoMapperFix` + registratie in `Cli.cs` + `CliTests`.
- [x] 2. Proof-runner (`ProofTests`, `TestReferences.AutoMapper`).
- [x] 3. Plan `docs/projectinfo/patterns/automapper-create-map-plan.md` (approved).
- [x] 4. `CreateMapPattern`, proofs (2 Fixed, 6 Manual), `CreateMapPatternTests`.
- [x] 5. Modellen, verify, README-rij, queue-rijen 0 en 1.
- [x] 6. Doortest SampleShop in `/mnt/dikkeschijf/unshackle-corpus/t146/`.

## Resultaat

- `dotnet build` 0 warnings, `dotnet test` 716/716, sample-tests 38/38, hook OK.
- Kleine Fixer-wijziging: `ReplaceWithTrivia` behoudt voorloop-regeleinden van extra nodes (witregel vóór `<Profile>Extensions`).

- SampleShop (kopie): 0 Fixed, 10 Manual (8× `Map`, 2 profiles met `ForMember`/`ReverseMap`/`ConvertUsing`), 0 bestanden gewijzigd; kopie bouwt, 38/38 tests groen.

## Zie ook

- `claude/tasks/automapper-queue.md`
- `docs/projectinfo/patterns/automapper-create-map-plan.md`
- `docs/projectinfo/patterns/automapper-create-map-verify.md`
