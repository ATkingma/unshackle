---
kingmadoc: 1
feature: "automapper-partial-migration"
status: approved
requirements:
    [
        REQ-1,
        REQ-2,
        REQ-3,
        REQ-4,
        REQ-5,
        REQ-6,
        REQ-7,
        REQ-8,
        REQ-9,
        REQ-10,
        REQ-11,
    ]
files_expected:
    - src/Unshackle.Core/Abstractions/IFix.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapCallPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapIntoPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ProjectToPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapBlockers.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/NestedMaps.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/AddAutoMapperPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/ConfigurationTestPattern.cs
    - src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapperCleanupPattern.cs
    - src/Unshackle.Tests/Core/MapOwnersTests.cs
    - src/Unshackle.Tests/Proofs/AutoMapper/_Combined/
    - src/Unshackle.Tests/CombinedProofTests.cs
---

# Feature: AutoMapper partial migration

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-17 by KingmaDoc |

> Drawn up from TAAK-272 and the analysis in TAAK-271. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

Instead of all or nothing per solution, every `CreateMap` pair gets one owner, the generated `ToB()` or AutoMapper 14, so one
Manual map no longer blocks the rest of the solution.

## Scope (in / out)

**In scope**

- A solution-wide plan (`MapOwners`) that decides per `CreateMap` statement (forward pair plus its `ReverseMap`) whether it is
  migrated or kept on AutoMapper, with the reason it is kept.
- Dependency closure: a kept map keeps every map AutoMapper needs for it (nested members, collection elements, `MapFrom`
  results, `Include`/`IncludeBase`); a migrated map whose `ToB()` calls the `ToY()` of a kept map is kept too.
- Call constraints: a `Map`, `Map(source, destination)` or `ProjectTo` call that cannot be rewritten keeps its pair; a call
  whose types are unknown (`object`, a type parameter) keeps every pair it may reach, or everything.
- Trimmed profiles: the kept `CreateMap` statements stay in the profile constructor; the migrated ones become `ToB()`.
- `IMapper` clean-up: a class whose `Map`/`ProjectTo` calls are all rewritten loses its `IMapper` field, constructor parameter
  and assignment, and the arguments at its `new` calls; then `AddAutoMapper` can go.
- Classes whose `IMapper` is mocked in a test (Moq, NSubstitute, FakeItEasy) keep their calls on AutoMapper; mock set-ups
  (`Setup(m => m.Map<B>(...))`, `Verify`, `.Returns`) are never rewritten.
- `AutoMapperFix` stops being all-or-nothing for planned keeps; unexpected Manual findings (internal errors, inactive `#if`
  code, non-UTF-8 files, overlaps) still undo the whole fix.

**Out of scope**

- The AutoMapper-specific blockers themselves (destination-only cycles, the unflattening false positive, null sources, private
  setters): TAAK-273.
- Removing the AutoMapper package from project files (the fix has no `Package`; emptied profiles keep it).
- Primary-constructor `IMapper` parameters and `IMapper` fields used outside their class (they stay, the registration with them).

## Requirements

- **REQ-1**: WHEN a profile holds one `CreateMap` with a problem and others without THE SYSTEM SHALL rewrite the others to
  `ToB()` and leave the problem `CreateMap` in the profile constructor.
- **REQ-2**: WHEN a kept map has a member whose type pair has its own `CreateMap` THE SYSTEM SHALL keep that map on AutoMapper too.
- **REQ-3**: WHEN a migratable map's `ToB()` would call the `ToY()` of a kept map THE SYSTEM SHALL keep the migratable map.
- **REQ-4**: WHEN a `Map`/`MapInto`/`ProjectTo` call cannot be rewritten THE SYSTEM SHALL keep the map of its pair (and its
  closure), and report the call as kept with the reason.
- **REQ-5**: WHEN a `Map` call's source or destination type is unknown THE SYSTEM SHALL keep every pair it may reach (all pairs
  when the destination is unknown).
- **REQ-6**: WHEN a call uses a kept pair THE SYSTEM SHALL leave the call on `IMapper` and say why the pair stays.
- **REQ-7**: WHEN every `Map`/`ProjectTo` call through a class's private `IMapper` field is rewritten THE SYSTEM SHALL remove the
  field, its constructor parameter and assignment, and the argument at every `new` call of that constructor.
- **REQ-8**: WHEN a test passes a mocked `IMapper` into a class THE SYSTEM SHALL keep that class's calls (and their pairs) on
  AutoMapper and never rewrite a mock set-up.
- **REQ-9**: WHEN no `IMapper` reference is left after the clean-up THE SYSTEM SHALL remove `AddAutoMapper`; otherwise it stays.
- **REQ-10**: WHEN maps stay on AutoMapper THE SYSTEM SHALL keep `AssertConfigurationIsValid`, which still passes on the trimmed
  profiles (probe).
- **REQ-11**: WHEN a Manual finding is not a planned keep (internal error, inactive `#if` code, non-UTF-8 file, overlap) THE
  SYSTEM SHALL leave the solution as it was (the old all-or-nothing safety net).

## Assumptions

- AutoMapper 14 resolves a map per type pair at run time; a pair whose `CreateMap` stays behaves as before, independent of
  which other pairs were removed, as long as the pairs it reaches stay (probe, step 2 of TAAK-272).
- `Map<B>(object)` maps the run-time type: only the destination is known statically.
- The plan is computed from the solution a pattern sees. The Fixer changes one file at a time, and removing migrated maps does
  not change which components are kept (the kept set is a fixpoint), so every pattern reaches the same decision.

## Risks

- A call left on `IMapper` while its `CreateMap` is removed fails at run time. Limited by: the plan seeds a pair from every call
  the call patterns would not rewrite, using the same checks; any unexpected Manual finding still undoes the whole fix.
- Tests that mock `IMapper` and verify `Map` calls would fail when the class is rewritten. Limited by REQ-8.
- Over-approximate closure keeps more maps than needed (safe, smaller gain). Measured on the corpus.

## Models

See [automapper-partial-migration-modellen.md](automapper-partial-migration-modellen.md).

## Open questions

- [x] Unit of migration: one `CreateMap` statement (forward pair plus `ReverseMap`), not a profile (TAAK-271).
- [x] Mocked `IMapper`: keep the class's calls on AutoMapper (tests `Verify` the `Map` calls; rewriting them would fail).

## See also

- `claude/tasks/TAAK-272-automapper-partial-migration.md`, `claude/tasks/archief/TAAK-271-analyse-manual-blockers.md`
- [automapper-partial-migration-verify.md](automapper-partial-migration-verify.md)
