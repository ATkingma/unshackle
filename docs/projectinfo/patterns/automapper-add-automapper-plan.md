---
kingmadoc: 1
feature: "automapper-add-automapper"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/AddAutoMapper/
---

# Feature: automapper.add-automapper: services.AddAutoMapper(...) → line removed

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Project**   | Unshackle                                                         |
| **Status**    | Approved                                                          |
| **Generated** | 2026-10-12 by KingmaDoc (by hand, `kingmadoc` not available)      |

> Drawn up from TAAK-159.

## One-sentence summary

automapper.add-automapper: the statement `services.AddAutoMapper(...);` disappears, but only if nothing in the solution still names `IMapper`, `IMapperBase`, `IRuntimeMapper` or `IConfigurationProvider`.

## Scope (in / out)

**In scope**

- Every `AddAutoMapper` overload from the AutoMapper assembly as a standalone statement.
- A parameter that was only passed to `AddAutoMapper` gets `_ = p;` (signature and callers stay, no IDE0060).

**Out of scope (Manual)**

- An AutoMapper service is still used (field, constructor parameter, `GetRequiredService<IMapper>()`), also in another project (e.g. tests).
- The result of `AddAutoMapper` is used (expression body, chain, return).

## Design

`AddAutoMapperPattern` is an `ISolutionPattern`, just like `AddMediatRPattern`. `RewriteAsync` looks for references to the four service types in the whole solution with `SymbolFinder.FindReferencesAsync`; the sync `Rewrite` only looks in its own compilation. Unlike AddMediatR (which is converted to `AddMediator`) there is no replacement: Unshackle generates ordinary extension methods, so the registration is superfluous as soon as nobody asks DI for a mapper.

## Requirements

- **REQ-1**: WHEN `AddAutoMapper(...)` is a standalone statement and no mapper service is used any more THE SYSTEM SHALL remove the statement (proof `simple`).
- **REQ-2**: WHEN a parameter was only passed to `AddAutoMapper` THE SYSTEM SHALL leave `_ = p;` (proof `parameter`).
- **REQ-3**: IF `IMapper` (or another AutoMapper service) is still used anywhere in the solution THEN THE SYSTEM SHALL report Manual with the place (proof `mapper-injected`).
- **REQ-4**: IF the result of `AddAutoMapper` is used THEN THE SYSTEM SHALL report Manual (proof `result-used`).
- **REQ-5** (TAAK-289): THE SYSTEM SHALL not count a use of `IMapper` inside code that [automapper.unused-imapper](automapper-unused-imapper-plan.md) removes,
  and SHALL remove a top-level `AddAutoMapper(...)` statement with its global statement (`UnusedMapperPatternTests.FixAsync_TopLevelAddAutoMapper_IsRemoved`).

## Assumptions

- `MapCallPattern` leaves the `IMapper` injection in place (TAAK-157); after cleaning up by hand the registration disappears in a next run.
- The empty Profiles stay and keep the AutoMapper package needed; the package only goes in a later row.

## Risks

- `FindReferencesAsync` per service per call; slower for large solutions, but `AddAutoMapper` is rare.

## Models

Class diagram, sequence and decision tree: [automapper-add-automapper-modellen.md](automapper-add-automapper-modellen.md).

## Open questions

- None.
