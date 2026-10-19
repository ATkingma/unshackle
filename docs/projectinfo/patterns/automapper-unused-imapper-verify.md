# Verification: automapper-unused-imapper

|               |                                                                                                |
| ------------- | ---------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-unused-imapper-plan.md`](automapper-unused-imapper-plan.md) |
| **Status**    | Verified                                                                                       |
| **Generated** | 2026-10-19 by KingmaDoc skill (by hand, `kingmadoc` not available)                             |

> Verified by an AI agent against the code of branch `worktree-agent-ac55b759e0d16a786` (TAAK-289).

## Requirements

| REQ   | Verdict | Evidence                                                                                                   |
| ----- | ------- | ---------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | proof `UnusedMapper/assigned-only`                                                                         |
| REQ-2 | Met     | proof `_Combined/unused-imapper-registration`; 21 regenerated `_Combined` call proofs                      |
| REQ-3 | Met     | proof `UnusedMapper/test-factory`; `FixAsync_MapperOnlyForMigratedCalls_…` (named arguments, second project) |
| REQ-4 | Met     | proof `UnusedMapper/null-checks`                                                                           |
| REQ-5 | Met     | proof `UnusedMapper/test-factory`                                                                          |
| REQ-6 | Met     | proof `UnusedMapper/service-lookup`; `FixAsync_ServiceLookupWithNullCheck_RemovesBothWithAWarning`         |
| REQ-7 | Met     | `FixAsync_MapperUsedOtherwise_KeepsItAndTheRegistration` (property, `null` argument, signature clash)      |
| REQ-8 | Met     | `FixAsync_ServiceLookupNotRegisteredByAddAutoMapperOrNotANullCheck_KeepsTheLookup`                         |
| REQ-9 | Met     | `FixAsync_TopLevelAddAutoMapper_IsRemoved`; assistiva (top-level `Program.cs`) on the corpus               |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 1613 passed, 0 failed.
- Corpus `after` (6 repos, see `claude/tasks/CORPUSRUN-blind.md`): all build, 0 regressions; pi22317 and assistiva fully migrated.

## Deviations

- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.
- One finding per type instead of one per removed node (so no blank line is left behind).

## See also

- [plan](automapper-unused-imapper-plan.md), [modellen](automapper-unused-imapper-modellen.md)
