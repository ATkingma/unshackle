---
kingmadoc: 1
feature: "automapper-kept-map"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/KeptMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/KeptMap/
---

# Feature: automapper.kept-map: report the maps a partial migration leaves on AutoMapper

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-17 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-272 (feature [automapper-partial-migration](../features/automapper-partial-migration-plan.md)).

## One-sentence summary

automapper.kept-map: after the profile patterns trimmed a profile, each `CreateMap` statement left in its constructor is reported
as Manual with the reason the plan (`MapOwners`) keeps that map on AutoMapper; the pattern changes nothing.

## Scope (in / out)

**In scope**

- CreateMap statements of a profile whose `<Profile>Extensions` class sits in the same file (the profile was partly rewritten).

**Out of scope**

- A profile that keeps all its maps: the profile pattern reports it once (`Stays on AutoMapper: CreateMap<A, B>: …`).
- Captured variables and other statements without a `CreateMap` pair.

## Design

`KeptMapPattern` (an `ISolutionPattern`) runs after the four profile patterns and before the call patterns. `Find` is syntactic;
`RewriteAsync` asks `MapOwners.ForAsync` for the reason of the statement's pair. The result carries the code `KeptReason.MapKept` (TAAK-295; the report prints the "Stays on AutoMapper: " prefix), so
`AutoMapperFix.AllowsPartial` lets the rest of the solution be migrated.

## Requirements

- **REQ-1**: WHEN a partly rewritten profile still holds `CreateMap<A, B>` THE SYSTEM SHALL report it as Manual with the reason the
  plan keeps the pair (proof `trimmed-profile`).
- **REQ-2**: THE SYSTEM SHALL change no code.
- **REQ-3**: THE SYSTEM SHALL start the reason with `Stays on AutoMapper: ` so the finding is a planned keep, not a blocker.
- **REQ-4** (TAAK-279): WHEN a statement stays because a class under test keeps calling `IMapper` for its map (a shared pair, which also has its `ToB()`) THE SYSTEM SHALL say so in the reason ([automapper-mocked-imapper](../features/automapper-mocked-imapper-plan.md)).

## Assumptions

- The profile patterns leave exactly the kept statements in the constructor (see `CreateMapPattern.Trimmed`).

## Risks

- A profile whose own class name ends in `Extensions` next to a profile with that name would be read as rewritten: only noise in the report.

## Models

[automapper-kept-map-modellen.md](automapper-kept-map-modellen.md).

## Open questions

- None.

## See also

- [verify](automapper-kept-map-verify.md), `claude/tasks/archief/TAAK-272-automapper-partial-migration.md`
