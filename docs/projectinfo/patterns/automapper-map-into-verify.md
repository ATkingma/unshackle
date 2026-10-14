# Verification: automapper-map-into

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-map-into-plan.md`](automapper-map-into-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-12 by KingmaDoc skill (manually, `kingmadoc` not available)                    |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `MapIntoPattern.cs`, `CreateMapPattern.cs`, `MapCallPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/MapInto/` and `_Combined/map-into-profile`. Unit test: `src/Unshackle.Tests/Core/MapIntoPatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                                                             |
| ----- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | `existing-method` (`Map(a, b)` and `Map<A, B>(orders[0], dto)`)                                                                                     |
| REQ-2 | Met     | `_Combined/map-into-profile` (with Ignore and a second map without MapInto), unit test `FixAsync_ProfileInOtherFile_WritesMapIntoAndCallsItWithUsing` |
| REQ-3 | Met     | `collection`                                                                                                                                         |
| REQ-4 | Met     | `unknown-map`                                                                                                                                        |
| REQ-5 | Met     | `maybe-null-source`                                                                                                                                  |
| REQ-6 | Met     | `maybe-null-destination`                                                                                                                             |
| REQ-7 | Met     | `object-source`, `receiver-call`                                                                                                                     |
| REQ-8 | Met     | all existing CreateMap, ForMember and MapCall proofs unchanged and green                                                                             |

## Deviations

- Proof `CreateMap/map-into` (Manual) removed: `CreateMapPattern.Find` now leaves `Map(a, b)` to this pattern. REQ-8 of `automapper-map-call-plan.md` still mentions that proof.
- "Several profiles", "becomes internal" and "already has a member" have no proof of their own; code review only (same logic as `MapCallPattern`).
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are present.

## Validation results

| Check   | Command                                                     | Result                                                                   |
| ------- | ----------------------------------------------------------- | ------------------------------------------------------------------------ |
| Build   | `dotnet build`                                              | passed (0 warnings)                                                      |
| Tests   | `dotnet test`                                               | 756/791 green; the 35 failures are the known CRLF failures of clean main |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38/38)                                                           |
| Lint    | _none_                                                      | _not run_                                                                |

## See also

- [automapper-map-into-plan.md](automapper-map-into-plan.md)
- [automapper-map-into-modellen.md](automapper-map-into-modellen.md)
