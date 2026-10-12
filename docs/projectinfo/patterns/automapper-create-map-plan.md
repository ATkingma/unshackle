---
kingmadoc: 1
feature: "automapper-create-map"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/
---

# Feature: automapper.create-map: Profile met CreateMap<A,B>() zonder opties → ToB-extensiemethode

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-146.

## One-sentence summary

automapper.create-map: een `Profile` waarvan de constructor alleen `CreateMap<A, B>()` zonder opties bevat, krijgt een statische klasse `<Profile>Extensions` met per map `static B ToB(this A source) => new() { X = source.X, ... }`; de constructor wordt leeg.

## Scope (in / out)

**In scope**

- Top-level, niet-generieke, niet-partial `Profile`-subklasse met één constructor die alleen `CreateMap<A, B>();`-statements heeft.
- Bestemmingsleden die AutoMapper schrijft: publieke properties met publieke `set`/`init` en publieke niet-readonly velden, ook geërfd.
- `Map`/`ProjectTo`-aanroepen van AutoMapper: Manual, zodat de hele fix (AllOrNothing) niets wijzigt zolang AutoMapper nog mapt.

**Out of scope**

- `ForMember`, `ReverseMap`, `ConvertUsing` en andere opties (rijen 2, 3 en 7 van `claude/tasks/automapper-queue.md`): Manual.
- Het lege `Profile` verwijderen en het AutoMapper-pakket weghalen: pas als `map-call` en `add-automapper` er zijn.

## Requirements

- **REQ-1**: WHEN een profile alleen `CreateMap<A, B>()` zonder opties heeft THE SYSTEM SHALL per map een `ToB(this A source)` met alle gelijknamige leden van hetzelfde type schrijven en de constructor leegmaken (proof `simple`, ook struct-bestemming en velden).
- **REQ-2**: WHEN A of B `internal` is THE SYSTEM SHALL de klasse en methodes `internal` maken (proof `internal-types`).
- **REQ-3**: IF een statement geen `CreateMap<A, B>()` zonder opties is THEN THE SYSTEM SHALL Manual melden voor het hele profile (proof `options`).
- **REQ-4**: IF een bestemmingslid geen bronlid met exact dezelfde naam heeft (flattening, `GetX()`, andere hoofdletters) THEN THE SYSTEM SHALL Manual melden (proof `flattening`, unittest hoofdletters).
- **REQ-5**: IF de typen verschillen of het lid een collectie is THEN THE SYSTEM SHALL Manual melden (proofs `conversion`, `collection`).
- **REQ-6**: IF B geen publieke parameterloze constructor heeft of abstract is THEN THE SYSTEM SHALL Manual melden (proof `constructor`).
- **REQ-7**: WHEN AutoMapper's `Map`/`ProjectTo` aangeroepen wordt THE SYSTEM SHALL Manual melden, zodat de fix niets wijzigt (proof `map-call`; sinds TAAK-157 alleen `ProjectTo`, `Map(src, dest)` en `Map` met opties, proof `map-into`).

## Assumptions

- AutoMapper 14.0.0 wijst gelijke, niet-collectie typen direct toe (geen kopie); collecties kopieert hij naar een nieuwe instantie.
- Een get-only bestemmingslid zonder bronlid negeert AutoMapper; met bronlid kan hij het vullen (bestaande collectie), daarom Manual.

## Risks

- `Map`-aanroepen in een **ander** project dan dat van de profile vindt de Fixer wel (hij draait alle projecten), maar buiten de solution niet; daar zou een ontbrekende type-map pas tijdens runtime falen.
- `ToB` geeft bij `null` een `NullReferenceException`, waar `mapper.Map` `null` gaf: van belang voor rij 4 (`map-call`).

## Modellen

Klassendiagram, sequence en beslisboom: [automapper-create-map-modellen.md](automapper-create-map-modellen.md).

## Open questions

- Geen.
