# TAAK-146 — AutoMapper infrastructure and automapper.create-map

**Status**: ✅ completed
**Created**: 2026-10-11
**Goal**: row 0 (`AutoMapperFix`, registration, proof runner) and row 1 (`automapper.create-map`) of `claude/tasks/automapper-queue.md`.

## Context for the next agent

- Fix: `src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs`, registered in `src/Unshackle.Interface/Cli.cs` (composition root, also `list`).
- Proof runner: folder `AutoMapper` in `ProofTests.s_fixes`, `TestReferences.AutoMapper` (AutoMapper 14.0.0 + DI abstractions) for both before and after: an (empty) profile keeps using AutoMapper.
- Rows 2 and 3 (`ForMember` Ignore/MapFrom) extend `CreateMapPattern.Problem`/the statement check; they need no profile recognition of their own.

## Choices

- **The whole profile or nothing**: `Find` returns the profile class, not every statement. An extension method must be in a top-level static class; only replacing the class with `[profile, <Profile>Extensions]` can add it. One unsupported map → Manual for the whole profile.
- **`AllOrNothing = true` and `Map`/`ProjectTo` → Manual**: a removed `CreateMap` while `mapper.Map` still runs only fails at runtime (missing type map). Until row 4 exists the fix therefore changes nothing in a solution that calls `Map`. Row 4 removes that Manual.
- **Only an exactly equal name and type**, no collections: AutoMapper matches case-insensitively, flattens, calls `GetX()`, converts types and copies collections. All those cases → Manual with a reason.
- **Parameter `source`** instead of `a`: a type name like `Event` would otherwise give a keyword.
- **`public`/`internal`** follows the real accessibility of A and B (otherwise CS0051).
- **The empty profile stays, no `Package` migration**: removing it breaks `typeof(XProfile)`/`AddProfile<X>` in other projects. The package therefore stays needed; `IFix.Package` only comes with the profile, map and add-automapper rows.
- **A second run is idempotent**: an emptied profile is no longer found (otherwise a Manual via AllOrNothing would undo the first run).

## Steps

- [x] 1. `AutoMapperFix` + registration in `Cli.cs` + `CliTests`.
- [x] 2. Proof runner (`ProofTests`, `TestReferences.AutoMapper`).
- [x] 3. Plan `docs/projectinfo/patterns/automapper-create-map-plan.md` (approved).
- [x] 4. `CreateMapPattern`, proofs (2 Fixed, 6 Manual), `CreateMapPatternTests`.
- [x] 5. Models, verify, README row, queue rows 0 and 1.
- [x] 6. End-to-end test of SampleShop in `<corpus>/t146/`.

## Result

- `dotnet build` 0 warnings, `dotnet test` 716/716, sample tests 38/38, hook OK.
- Small Fixer change: `ReplaceWithTrivia` keeps leading line breaks of extra nodes (blank line before `<Profile>Extensions`).

- SampleShop (copy): 0 Fixed, 10 Manual (8× `Map`, 2 profiles with `ForMember`/`ReverseMap`/`ConvertUsing`), 0 files changed; the copy builds, 38/38 tests green.

## See also

- `claude/tasks/automapper-queue.md`
- `docs/projectinfo/patterns/automapper-create-map-plan.md`
- `docs/projectinfo/patterns/automapper-create-map-verify.md`
