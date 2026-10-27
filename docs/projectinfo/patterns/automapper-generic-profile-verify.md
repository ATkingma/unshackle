# Verification: automapper-generic-profile

|               |                                                                                                      |
| ------------- | ---------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-generic-profile-plan.md`](automapper-generic-profile-plan.md) |
| **Status**    | Verified (unit tests and proofs; corpus not re-measured)                                             |
| **Generated** | 2026-10-27 by KingmaDoc skill (by hand, `kingmadoc` not available)                                   |

> Verified by an AI agent against the TAAK-334 worktree branch.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericProfilePattern.cs`, `src/Unshackle.Core/Generics/TypeArgumentClosure.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/GenericProfile/`. Unit tests: `src/Unshackle.Tests/Core/AutoMapper/GenericProfilePatternTests.cs`.

| REQ | Verdict | Evidence |
| --- | ------- | -------- |
| REQ-1 | Met | `closed-subclasses` (before compiles against AutoMapper 14, after has no `CreateMap`), `FixAsync_TwoClosedSubclasses_WritesBothPairs` |
| REQ-2 | Met | `FixAsync_ClosedConstruction_WritesMethodPerPair` (subclass, `new`, `AddProfile`) |
| REQ-3 | Met | `open-registration` |
| REQ-4 | Met | `packable-library`, `skoruba-shape` (moved from `CreateMap/generic`), `FixAsync_PublicProfile_FollowsIsPackable(false, true)` |
| REQ-5 | Met | `FixAsync_UnsupportedMap_IsOpenSet` (`ReverseMap`, `List<T>`) |
| REQ-6 | Met | `FixAsync_NoClosedConstruction_IsOpenSet` |
| REQ-7 | Met | `GenericMap/packable-public-base`, `FixAsync_PublicMethodInLibraryWithProjectProperties_FollowsIsPackable(true)` |

## Deviations

- `TypeArgumentClosure` needed no change for class type parameters: `CloseByUses` (A11) already closes them; only the R1 reason changed (A13).
- `Map` calls on a closed pair are not routed to the generated methods yet (see plan, Risks); the proofs have no such call.
- No PNG/SVG of the models (no d2/kingmadoc).

## Validation results

| Check | Command | Result |
| ----- | ------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test --project src/Unshackle.Tests` (TEMP on D:) | 3822/3822 |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 |
| Corpus | skoruba, letterbook, personalasst | not run: the corpus disk had about 3 GB free (H1 threshold 20 GB) |

## See also

- [automapper-generic-profile-plan.md](automapper-generic-profile-plan.md)
- [automapper-generic-profile-modellen.md](automapper-generic-profile-modellen.md)
