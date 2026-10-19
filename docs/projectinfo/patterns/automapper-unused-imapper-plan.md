---
kingmadoc: 1
feature: "automapper-unused-imapper"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
files_expected:
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnusedMapperPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapperCleanup.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/UnusedMapper/
    - src/Unshackle.Tests/Core/UnusedMapperPatternTests.cs
---

# Feature: automapper.unused-imapper: remove the IMapper that only served migrated calls

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-19 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-289 (round 5 blocker: `AddAutoMapper` stays because `IMapper` is still injected).

## One-sentence summary

automapper.unused-imapper: an `IMapper` field, constructor parameter or local whose only uses are Map calls the fix migrates is removed,
with its assignments, null checks, the arguments for it at every caller and a static factory that only built it, so that
`automapper.add-automapper` can remove the registration.

## Scope (in / out)

**In scope**

- Fields, constructor parameters (of classes, not records or primary constructors) and locals of type `IMapper`, `IMapperBase` or `IRuntimeMapper`.
- The code that only exists for them: `_mapper = mapper;`, `_mapper = mapper ?? throw …;`, `ArgumentNullException.ThrowIfNull(mapper);`,
  `if (mapper == null) …` without `else`, the argument at each `new C(...)`, `new(...)`, `: this(...)` / `: base(...)` (positional or named),
  and `static IMapper Create()` factories whose body only declares locals and returns.
- Callers in other projects of the solution (tests constructing the class, DI factory lambdas, seeders).

**Out of scope**

- Properties of type `IMapper`, method parameters other than constructor ones, `?.Map(...)`, and any other use (`mapper.ConfigurationProvider`,
  passing it to a method): the symbol stays, and with it `AddAutoMapper`.
- A value the removal would drop that may do something else (any call other than `CreateMapper()`, `new Mapper(..)`, `new MapperConfiguration(..)`,
  a removed factory or a DI lookup): the symbol stays.
- Removing the now empty constructor or factory class: they stay (DI needs a public constructor; an empty static class is harmless).

## Design

`MapperCleanup` builds one plan per solution (cached like `MapOwners.ForAsync`, reused while the solution differs only in the file it was
built from and the file being fixed). It is a greatest fixed point: start with every candidate and drop the ones with a use that is neither
a migrated call (found by `MapCallPattern`/`MapIntoPattern`/`ProjectToPattern` and not kept by `MapOwners.KeptCall`) nor inside code the
plan removes for the current set, until nothing changes. Symbols are keyed by documentation comment id, so the plan holds across
compilations (a test project sees the service through a project reference).

`UnusedMapperPattern` (an `ISolutionFinder` + `ISolutionPattern`) runs after the call patterns and before `AddAutoMapperPattern`. It returns
the outermost type that holds removals (or the removal itself outside a type) and rewrites it in one go, so a removed first member or statement
leaves no blank line. `AddAutoMapperPattern` ignores an `IMapper` reference that lies inside code the plan removes; it now also removes a
top-level `AddAutoMapper(...)` statement (`GlobalStatementSyntax`), which used to end in an internal error.

## Requirements

- **REQ-1**: WHEN an `IMapper` field and constructor parameter are only assigned THE SYSTEM SHALL remove both and the assignment (proof `assigned-only`).
- **REQ-2**: WHEN the field's only other uses are Map calls the fix migrates THE SYSTEM SHALL remove it as well, and `AddAutoMapper` with it
  (proofs `_Combined/unused-imapper-registration`, `_Combined/map-call-profile` and the other `_Combined` call proofs).
- **REQ-3**: THE SYSTEM SHALL remove the argument for a removed parameter at every caller in the solution, named or positional, in other projects too
  (proof `test-factory`; `UnusedMapperPatternTests.FixAsync_MapperOnlyForMigratedCalls_…`).
- **REQ-4**: THE SYSTEM SHALL remove a null check on a removed parameter or local: `ThrowIfNull`, `if (x == null) throw`, `x ?? throw` (proof `null-checks`).
- **REQ-5**: WHEN a field or local only held a mapper built by a static factory, `CreateMapper()` or a DI lookup THE SYSTEM SHALL remove it, and the factory
  once nothing else calls it (proof `test-factory`).
- **REQ-6**: WHEN a `GetService<IMapper>()` lookup is followed by `if (mapper == null) { …; return; }` without `else`, and the solution calls
  `AddAutoMapper`, THE SYSTEM SHALL remove the lookup and the check and attach a warning to the finding (proof `service-lookup`;
  `FixAsync_ServiceLookupWithNullCheck_RemovesBothWithAWarning`).
- **REQ-7**: WHEN the mapper is used otherwise, a caller passes a value that is not droppable (`null`), or removing the parameter would leave two
  constructors with one signature THE SYSTEM SHALL keep it (`FixAsync_MapperUsedOtherwise_KeepsItAndTheRegistration`).
- **REQ-8**: WHEN the solution does not call `AddAutoMapper`, or the check is not a plain null check, THE SYSTEM SHALL keep the DI lookup
  (`FixAsync_ServiceLookupNotRegisteredByAddAutoMapperOrNotANullCheck_KeepsTheLookup`).
- **REQ-9**: WHEN `AddAutoMapper` is a top-level statement THE SYSTEM SHALL remove it like any other (`FixAsync_TopLevelAddAutoMapper_IsRemoved`).

## Assumptions

- AutoMapper's fix is all-or-nothing (`AutoMapperFix.AllOrNothing`): a call the plan counts as migrated that ends up Manual undoes the whole fix, so the
  plan only has to know which calls are kept (`MapOwners`), not whether each rewrite succeeds.
- Building a mapper (`new MapperConfiguration(cfg => cfg.AddProfile<P>())`, `CreateMapper()`, a factory) has no effect the code relies on.
- A `GetService<IMapper>()` null check only took its branch when nothing registered `IMapper`; while the solution calls `AddAutoMapper`
  that branch did not run. This is the reviewed assumption of the lost first attempt; it is kept, but only under the registration gate,
  only for `== null`/`is null` checks without `else`, and the finding carries a warning so a reviewer sees it.

## Risks

- A service provider built without the `AddAutoMapper` call of the solution (a second container) would have taken the removed branch.
  Mitigated by the warning; open question for the user below.
- A constructor called by reflection with explicit arguments (not DI) would break; such a call is not visible in the solution.

## Models

[automapper-unused-imapper-modellen.md](automapper-unused-imapper-modellen.md).

## Open questions

- Keep the null-check removal (REQ-6) with its warning, or treat any check on a DI lookup as "keep the lookup and the registration"?
  The current choice turns pi22317 fully migrated; the stricter one leaves `AddAutoMapper` in its `AccountController`.

## See also

- [verify](automapper-unused-imapper-verify.md), [automapper-add-automapper-plan](automapper-add-automapper-plan.md),
  `claude/tasks/archief/TAAK-289-automapper-imapper-still-injected.md`
