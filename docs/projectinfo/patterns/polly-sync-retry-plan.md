---
kingmadoc: 1
feature: "polly-sync-retry"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/SyncRetryPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/SyncRetry/
---

# Feature: polly.sync-retry: Retry, WaitAndRetry(Forever) + Execute → AddRetry

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 4 (`claude/tasks/polly-queue.md`), council round 8 general rule, P2/P3.

## One-sentence summary

polly.sync-retry: the v7 synchronous retry methods, held in a local, a private field or inline and run through `Execute`, become
the same `ResiliencePipelineBuilder(<T>)().AddRetry(...)` pipeline as the async patterns, and the `Execute` call stays as written.

## Scope (in / out)

**In scope**: `Retry()`, `Retry(n)`, `RetryForever()`, `WaitAndRetry(n, Func<int, TimeSpan>)`, `WaitAndRetry(durations)`,
`WaitAndRetryForever(Func<int, TimeSpan>)`, each with `onRetry`; `Execute(Action)` and `Execute<T>(Func<T>)`; the filter chains,
generic pipelines, TFM check, fresh names and holder rules of `polly.wait-and-retry-async` (shared `PollyRetry`).

**Out of scope**: `Execute` with `Context`, `CancellationToken` or a dictionary argument (Manual `ExecuteOverloadNotSupported`),
`ExecuteAndCapture` (queue row 12), sync `Wrap`, Context- or outcome-taking sleep-duration providers.

## Requirements

- **REQ-1**: WHEN a sync retry call has a literal count (or none, or Forever) and a plain delay lambda THE SYSTEM SHALL emit the same
  `RetryStrategyOptions` as the async twin (proofs `wait-and-retry`, `forever`, `retry-on-retry`).
- **REQ-2**: WHEN the policy is only used as the receiver of `Execute(Action)` / `Execute<T>(Func<T>)` THE SYSTEM SHALL leave the
  `Execute` argument untouched, because v8 has the same two overloads (proofs `wait-and-retry`, `handle-result`).
- **REQ-3**: WHEN `HandleResult<T>` / `Policy<T>` is used THE SYSTEM SHALL emit a `ResiliencePipeline<T>` (proof `handle-result`).
- **REQ-4**: THE SYSTEM SHALL give equal run-time traces before (Polly 7.2.4) and after (Polly.Core) for every Fixed proof
  (`PollyBehaviourProofTests`).
- **REQ-5**: WHEN `Execute` takes a Context, a token or a dictionary, or the policy is used for more than `Execute`, THE SYSTEM SHALL
  report Manual `ExecuteOverloadNotSupported` / `PolicyUsedBeyondExecute` (proofs `context`, `policy-used-beyond`).

## Assumptions

- v8's synchronous `Execute` waits the delay on the calling thread like v7's `Thread.Sleep`; the proofs pin the observable trace, not the thread.

## Models

[polly-sync-retry-modellen.md](polly-sync-retry-modellen.md).

## Open questions

- None.
