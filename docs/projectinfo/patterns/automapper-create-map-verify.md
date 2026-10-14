# Verification: automapper-create-map

|               |                                                                                            |
| ------------- | ------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-create-map-plan.md`](automapper-create-map-plan.md) |
| **Status**    | Verified                                                                                   |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                        |

> Verified by an AI agent against the code at uncommitted changes (basis 5bca3cb).

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, `AutoMapperFix.cs`, registratie in `src/Unshackle.Interface/Cli.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/`. Unittests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                        |
| ----- | ------- | ----------------------------------------------------------------------------- |
| REQ-1 | Met | `simple` (properties, `init`, veld, struct-bestemming, get-only overgeslagen) |
| REQ-2 | Met | `internal-types`                                                              |
| REQ-3 | Met | `options`                                                                     |
| REQ-4 | Met | `flattening`, unit test for case                                             |
| REQ-5 | Met | `conversion`, unittest `int[]`; `collection` is rewritten since TAAK-245 (see [automapper-nested-maps-verify.md](automapper-nested-maps-verify.md)) |
| REQ-6 | Met | `constructor`, unittest abstract                                              |
| REQ-7 | Met | `map-into`                                                                    |

End-to-end test of SampleShop (copy in `<corpus>/t146/`): 0 Fixed, 10 Manual (8× `Map`, 2 profiles with options), 0 files changed (AllOrNothing); the copy builds and 38/38 tests are green.

## Deviations

- Parameter name `source` instead of `a` from the queue: avoids keywords as a parameter name.
- `Fixer.ReplaceWithTrivia` now keeps blank lines that a pattern puts before an extra node (otherwise the new class came directly after the profile without a blank line).
- The empty profile stays and the package too (see plan, Out of scope).

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 716/716 green                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green                             |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## See also

- [automapper-create-map-plan.md](automapper-create-map-plan.md)
- [automapper-create-map-modellen.md](automapper-create-map-modellen.md)
