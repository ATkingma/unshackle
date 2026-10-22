# Verification: polly-retry-variants

|               |                                                                                          |
| ------------- | ---------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/polly-retry-variants-plan.md`](polly-retry-variants-plan.md) |
| **Status**    | Verified                                                                                 |
| **Generated** | 2026-10-22 by KingmaDoc                                                                  |

> Verified by an AI agent (polly-skeleton, TAAK-308 queue row 7) against the code on its worktree branch.

## Requirements

Code: `src/Unshackle.Core/Fixes/Polly/Patterns/RetryVariantsPattern.cs`, `PollyRetry.cs`, registration in `PollyFix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/Polly/RetryVariants/` (text: `ProofTests`; run-time: `PollyBehaviourProofTests`).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `retry-count` (with `Action<Exception, int>` onRetry), `retry-default` (inline) |
| REQ-2 | Met | `wait-forever` (`WaitAndRetryForeverAsync` on a result filter, `RetryForeverAsync`) |
| REQ-3 | Met | `durations` (3 literal durations, onRetry sees 0, 1, 2 ms), `durations-variable`; unit test with a variable element |
| REQ-4 | Met | `retry-count`, `durations`; unit test `OnRetryUsesContext` on `RetryForeverAsync` |
| REQ-5 | Met | `Proof_BeforeAndAfter_SameTrace` for all four Fixed proofs |
| REQ-7 | Met | `field-instance` (+ trace), `Rewrite_FieldNotProvablyLocal_Manual` |
| REQ-6 | Met | unit test `RetryAsync(n)` with a variable |

`Find_EachRetryCallBelongsToExactlyOnePattern` checks the split between this pattern and `polly.wait-and-retry-async`.

## Deviations

None.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test` | all green (see TAAK-308 log) |

## See also

- [polly-retry-variants-plan.md](polly-retry-variants-plan.md)
- [polly-retry-variants-modellen.md](polly-retry-variants-modellen.md)
