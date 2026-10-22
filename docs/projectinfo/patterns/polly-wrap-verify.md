# Verification: polly-wrap

|               |                                                                      |
| ------------- | -------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/polly-wrap-plan.md`](polly-wrap-plan.md) |
| **Status**    | Verified                                                             |
| **Generated** | 2026-10-22 by KingmaDoc                                              |

> Verified by an AI agent (polly-skeleton, TAAK-308 queue row 10) against the code on its worktree branch.

## Requirements

Code: `WrapPattern.cs`, `PollyWrap.cs`, `PollyRetry.PlanWrapUse`; registration in `PollyFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/Polly/Wrap/`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | after texts of `two-locals` and `three-inline` |
| REQ-2 | Met | traces `inner 1, inner 2, outer 1, inner 1, done` and `argument 1, invalid 1, timeout 1, argument 1, invalid 1, done` equal before/after; `WrapProof_OrderReversed_DifferentTrace` × 2 |
| REQ-3 | Met | `two-locals` (both operand locals gone) |
| REQ-4 | Met | `operand-reused` |
| REQ-5 | Met | `Rewrite_GenericWrap_FixedWithTypedBuilder` |
| REQ-6 | Met | `Find_WrapOperands_LeftToTheWrapPattern` |

## Deviations

None.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test` | all green (see TAAK-308 log) |

## See also

- [polly-wrap-plan.md](polly-wrap-plan.md)
- [polly-wrap-modellen.md](polly-wrap-modellen.md)
