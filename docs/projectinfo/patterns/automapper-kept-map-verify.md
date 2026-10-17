# Verification: automapper-kept-map

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-kept-map-plan.md`](automapper-kept-map-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-17 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-a50ecf5da4db68373` (TAAK-272).

## Requirements

| REQ   | Verdict | Evidence                                                                                          |
| ----- | ------- | ------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | proof `KeptMap/trimmed-profile`; `MapOwnersTests.FixAsync_OneMapWithAProblem_KeepsOnlyThatMapOnAutoMapper` |
| REQ-2 | Met     | the proof checks the text is unchanged                                                            |
| REQ-3 | Met     | `AutoMapperFix.AllowsPartial`; `_Combined/partial-*` proofs accept only these Manual rows         |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all green (see the task file for the count).

## Deviations

- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## See also

- [plan](automapper-kept-map-plan.md), [modellen](automapper-kept-map-modellen.md)
