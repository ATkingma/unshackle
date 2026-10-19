# Verification: automapper-unused-imapper

|               |                                                                                                |
| ------------- | ---------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-unused-imapper-plan.md`](automapper-unused-imapper-plan.md) |
| **Status**    | Verified                                                                                       |
| **Generated** | 2026-10-19 by KingmaDoc skill (by hand, `kingmadoc` not available)                             |

> Verified by an AI agent against the code of branch `worktree-agent-ac55b759e0d16a786` (TAAK-289); REQ-6, REQ-8 and REQ-10..12 re-verified on branch `worktree-agent-ad17ff04cdf5c5b21` (TAAK-292); REQ-8, REQ-10 and REQ-13..15 on branch `worktree-agent-af3ef946941f5cd53` (TAAK-293).

## Requirements

| REQ   | Verdict | Evidence                                                                                                   |
| ----- | ------- | ---------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | proof `UnusedMapper/assigned-only`                                                                         |
| REQ-2 | Met     | proof `_Combined/unused-imapper-registration`; 21 regenerated `_Combined` call proofs                      |
| REQ-3 | Met     | proof `UnusedMapper/test-factory`; `FixAsync_MapperOnlyForMigratedCalls_…` (named arguments, second project) |
| REQ-4 | Met     | proof `UnusedMapper/null-checks`                                                                           |
| REQ-5 | Met     | proof `UnusedMapper/test-factory`                                                                          |
| REQ-6 | Met     | proof `UnusedMapper/service-lookup`; `FixAsync_ServiceLookupInTheRegisteringProject_RemovesItAndTheNullCheckWithAWarning` (warning names `AddAutoMapper at Shop.cs:<line>`) |
| REQ-7 | Met     | `FixAsync_MapperUsedOtherwise_KeepsItAndTheRegistration` (property, `null` argument, signature clash)      |
| REQ-8 | Met     | `FixAsync_ServiceLookupNotSurelyRegistered_KeepsTheLookupAndNamesTheRule` (8 rows, each with its `KeptReason`), `FixAsync_ServiceLookupNotANullCheck_KeepsTheLookup` |
| REQ-9 | Met     | `FixAsync_TopLevelAddAutoMapper_IsRemoved`; assistiva (top-level `Program.cs`) on the corpus               |
| REQ-10 | Met    | `FixAsync_ServiceLookupRegisteredByADependentHost_RemovesIt` (rule 1, dependent host); theory rows `""`/`in shop` (rule 1), `if`/`#if` (rule 2), `container` (rule 3); proofs `service-lookup-conditional-registration`, `service-lookup-second-container` |
| REQ-11 | Met    | proof `empty-factory-class`; `FixAsync_FactoryClassLeftEmpty_KeepsItAndReportsAnInfoLine`; `ReportWriterTests.From_FixedWithWarningAndInfo_PutsBothInReason` |
| REQ-12 | Met    | `AutoMapperFix_PatternOrder_CallPatternsThenUnusedMapperThenAddAutoMapper`, `FixAsync_OneRemainingMapperUse_KeepsAddAutoMapper`, `FixAsync_OverloadsAndPartialType_RemovesEveryMapper` |
| REQ-13 | Met    | `ReportWriterTests.From_KeptLookup_RendersThePrefixInFrontOfTheReason` (pins `Stays on AutoMapper: `), `JsonAndMarkdownWriteAsync_FixedWithInfo_WriteTheInfoLine`; the theory asserts `ManualResult.Kept` and a reason without prefix; `AutoMapperFix.AllowsPartial` accepts the code |
| REQ-14 | Met    | theory row `target-typed` (rule 3, `ContainerWithoutRegistration`); proof `service-lookup-target-typed-container` |
| REQ-15 | Met    | theory rows `stored lambda`, `local function` (`ConditionalOrDeferredRegistration`); `FixAsync_AddAutoMapperInALambdaPassedToARegistrationCall_RemovesTheLookup`; proof `service-lookup-deferred-registration` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 1613 passed, 0 failed (TAAK-289); 1635 passed, 0 failed (TAAK-292); 1644 passed, 0 failed (TAAK-293).
- Corpus `after` (6 repos, see `claude/tasks/CORPUSRUN-blind.md`): all build, 0 regressions; pi22317 and assistiva fully migrated.

## Deviations

- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there. TAAK-292 changed figure 1 and added figure 4 (D2 only, render pending).
- Line numbers in the warning and Manual reasons (`file:line`) are those of the tree at the moment the pattern runs: earlier patterns of the same run may have shifted them by a few lines.
- One finding per type instead of one per removed node (so no blank line is left behind).

## See also

- [plan](automapper-unused-imapper-plan.md), [modellen](automapper-unused-imapper-modellen.md)
