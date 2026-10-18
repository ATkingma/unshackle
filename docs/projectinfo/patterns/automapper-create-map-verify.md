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
| REQ-5 | Met | `conversion-operator` (until TAAK-247: `conversion`), unittest `int[]`; `collection` is rewritten since TAAK-245 (see [automapper-nested-maps-verify.md](automapper-nested-maps-verify.md)) |
| REQ-6 | Met | `constructor`, unittest abstract                                              |
| REQ-7 | Met | `map-into`                                                                    |
| REQ-8 | Met | `unmatched-member`, `ForMemberIgnore/unmatched` (Fixed); `unmatched-asserted`, `private-setter`, `get-only-collection` (Manual); unit tests `Rewrite_MemberAutoMapperDoesNotWrite_LeftOut` |
| REQ-9 | Met | `private-setter` (reason says principally manual) (TAAK-273) |
| REQ-10 | Met | `broken-conversion` (TAAK-273) |
| REQ-12 | Met | `_Combined/required-members`, `_Combined/project-to-required`; unit tests `Rewrite_RequiredMember*`; probe against AutoMapper 14.0.0 (TAAK-282): unmapped required members stay `default`, an initializer or constructor value stays |
| REQ-13 | Met | `_Combined/region-directives`; unit test `Rewrite_DirectivesAroundCreateMap_StayBalancedOrManual` (TAAK-280) |
| REQ-14 | Met | `_Combined/configuration-maps-map-call`; unit tests `ConfigurationMapsTests` (TAAK-286); hospitalappt 0 / 32 → 6 / 35 Fixed/Manual, 0 regressions |

End-to-end test of SampleShop (copy in `<corpus>/t146/`): 0 Fixed, 10 Manual (8× `Map`, 2 profiles with options), 0 files changed (AllOrNothing); the copy builds and 38/38 tests are green.

TAAK-261 measurement (AutoMapper 14.0.0, `CreateMap<S, D>()`): a destination member without a source keeps its initializer (`"init"`) or default (`0`), and only `AssertConfigurationIsValid` reports it ("Unmapped properties"); `{ get; private set; }` and `{ get; protected set; }` are written from the source; get-only `List<int>` and `IReadOnlyList<int>` (backed by a list) end with exactly the source items; a get-only `int[]`, `string`, computed property, `readonly` field and get-only object with its own map keep their value; `S? P` is not flattened into `PX`.

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

- TAAK-279 (REQ-11): verified in [automapper-mocked-imapper-verify](../features/automapper-mocked-imapper-verify.md) (unit tests, proofs, corpus 0 regressions).

## See also

- [automapper-create-map-plan.md](automapper-create-map-plan.md)
- [automapper-create-map-modellen.md](automapper-create-map-modellen.md)
