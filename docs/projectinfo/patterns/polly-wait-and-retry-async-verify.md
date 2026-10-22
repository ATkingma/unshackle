# Verification: polly-wait-and-retry-async

|               |                                                                                                      |
| ------------- | ---------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/polly-wait-and-retry-async-plan.md`](polly-wait-and-retry-async-plan.md) |
| **Status**    | Verified                                                                                             |
| **Generated** | 2026-10-22 by KingmaDoc                                                                              |

> Verified by an AI agent (polly-skeleton, TAAK-308 step 4) against the code on its worktree branch (basis 511f434).

## Requirements

Code: `src/Unshackle.Core/Fixes/Polly/Patterns/WaitAndRetryAsyncPattern.cs`, `PollyRetry.cs`, registration in `PollyFix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/Polly/WaitAndRetryAsync/` (text: `ProofTests`; run-time: `Core/Polly/PollyBehaviourProofTests`).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `local-block-delay`, `inline-result`, `typed-local-not-handled` (after compiles against Polly.Core only) |
| REQ-2 | Met | same proofs; `Proof_WithoutAttemptOffset_DifferentTrace` (trace `delay 0, 1, 2` without `+ 1`) |
| REQ-3 | Met | `local-block-delay`, `inline-result` (async); `typed-local-not-handled` (`new ValueTask(...)`); `Rewrite_GenericResultFromTaskExpression_WrapsInTypedValueTask` |
| REQ-4 | Met | `typed-local-not-handled` (`IAsyncPolicy` → `ResiliencePipeline`) |
| REQ-5 | Met | `Proof_BeforeAndAfter_SameTrace` × 3: exhausted retries, success on attempt 3 with result, unhandled exception on the first call (sync throw from a Task-returning method) |
| REQ-6 | Met | `retry-count-variable`; unit test with literal `0` |
| REQ-7 | Met | `result-not-awaited`; unit tests `PolicyUsedBeyondExecute`, `ExecuteOverloadNotSupported`, `ExecuteNotLambda` |
| REQ-8 | Met | `policy-returned` |
| REQ-9 | Met | `filter-or-predicate`, `filter-handle-result`, `filter-policy-of-t` (+ behaviour traces), `filter-handle-inner`; unit tests `HandleInner`, `HandleResult(1)` |
| REQ-10 | Met | `filter-handle-result` (trace ends `result -4`), `filter-policy-of-t` (`ResiliencePipeline<string>`) |
| REQ-11 | Met | `Rewrite_UnsupportedTargetFramework_Manual` (netcoreapp3.0, net452) |
| REQ-12 | Met | `Rewrite_NameTakenInScope_UsesFreshName` (`args1`) |
| REQ-13 | Met | `on-retry-attempt` (named `onRetry:`, unused Context; trace pins delay → onRetry order and the 1-based count), `on-retry-async`, `on-retry-result` |
| REQ-14 | Met | `on-retry-context`; unit test `OnRetryNotMappable` (DelegateResult passed on) |

## Deviations

- The reason code is the leading word of the Manual text (`RetryCountNotLiteral: ...`), not a typed field: `ManualResult` has no
  code field outside `KeptReason` yet (council question in TAAK-308).

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test` | all green (row 2: see TAAK-308 log) |
| Corpus | — | not run yet (TAAK-308 step 6) |

## See also

- [polly-wait-and-retry-async-plan.md](polly-wait-and-retry-async-plan.md)
- [polly-wait-and-retry-async-modellen.md](polly-wait-and-retry-async-modellen.md)
