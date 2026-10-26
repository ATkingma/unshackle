# Verification: polly-timeout

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/polly-timeout-plan.md`](polly-timeout-plan.md) |
| **Status**    | Verified                                                          |
| **Generated** | 2026-10-22 by KingmaDoc                                           |

> Verified by an AI agent (polly pool agent L1, TAAK-308 queue row 9) against the code on its worktree branch.

## Requirements

Code: `src/Unshackle.Core/Fixes/Polly/Patterns/TimeoutPattern.cs`, `PollyRetry.cs`, registration in `PollyFix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/Polly/` (text: `ProofTests`; run-time: `PollyBehaviourProofTests`).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `token-honoured`, `token-ignored`, `int-seconds`, `sync`, `typed` |
| REQ-2 | Met | `Proof_BeforeAndAfter_SameTrace` for the five Fixed proofs |
| REQ-3 | Met | `pessimistic` |
| REQ-4 | Met | `too-small`, `variable`, unit tests |
| REQ-5 | Met | `on-timeout`, unit test |
| REQ-6 | Met | `token-sync-lambda` |

## Deviations

None.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (see TAAK-308 log) |
| Tests | `dotnet test` | all green (see TAAK-308 log) |

## See also

- [polly-timeout-plan.md](polly-timeout-plan.md)
- [polly-timeout-modellen.md](polly-timeout-modellen.md)
