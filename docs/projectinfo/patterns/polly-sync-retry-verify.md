# Verification: polly-sync-retry

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/polly-sync-retry-plan.md`](polly-sync-retry-plan.md)   |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc                                                            |

> Verified by an AI agent (polly pool agent L1, TAAK-308 queue row 4) against the code on its worktree branch.

## Requirements

Code: `src/Unshackle.Core/Fixes/Polly/Patterns/SyncRetryPattern.cs`, `PollyRetry.cs`, registration in `PollyFix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/Polly/SyncRetry/` (text: `ProofTests`; run-time: `PollyBehaviourProofTests`).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `wait-and-retry`, `forever`, `retry-on-retry` |
| REQ-2 | Met | `wait-and-retry` (`Execute(Action)` and `Execute<int>`), `handle-result`; `Rewrite_InlineRetryAndExecute_ExecuteCallKept` |
| REQ-3 | Met | `handle-result` |
| REQ-4 | Met | `Proof_BeforeAndAfter_SameTrace` for the four Fixed proofs: traces equal on the first run |
| REQ-5 | Met | `context`, `policy-used-beyond`; unit test `Rewrite_UnsupportedShape_EveryFindingManualWithCode` |

`Find_SyncAndAsyncRetryCallsBelongToDifferentPatterns` checks the split with the two async patterns.

## Deviations

The queue expected `Execute(_ => ...)`; v8 has `Execute(Action)` and `Execute<T>(Func<T>)`, so the callback needs no change.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (see TAAK-308 log) |
| Tests | `dotnet test` | all green (see TAAK-308 log) |

## See also

- [polly-sync-retry-plan.md](polly-sync-retry-plan.md)
- [polly-sync-retry-modellen.md](polly-sync-retry-modellen.md)
