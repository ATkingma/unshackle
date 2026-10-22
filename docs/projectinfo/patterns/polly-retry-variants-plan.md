---
kingmadoc: 1
feature: "polly-retry-variants"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/RetryVariantsPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/RetryVariants/
---

# Feature: polly.retry-variants: RetryAsync, RetryForeverAsync, WaitAndRetryForeverAsync, WaitAndRetryAsync(durations) → AddRetry

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 7 (`claude/tasks/polly-queue.md`), council round 8 general rule and P2/P3, round 10 P9/P10.

## One-sentence summary

polly.retry-variants: the other v7 async retry methods, held in a local or inline and run through awaited `ExecuteAsync`, become
the same `ResiliencePipelineBuilder(<T>)().AddRetry(...)` pipeline as `polly.wait-and-retry-async`, with only `MaxRetryAttempts`
and the delay differing.

## Scope (in / out)

**In scope**: `RetryAsync()`, `RetryAsync(n)`, `RetryForeverAsync()`, `WaitAndRetryForeverAsync(Func<int, TimeSpan>)`,
`WaitAndRetryAsync(IEnumerable<TimeSpan>)` with a literal list; each with an optional `onRetry`/`onRetryAsync`; the filter
chains, generic pipelines, TFM check, fresh names and local/inline rules of `polly.wait-and-retry-async` (shared `PollyRetry`).

**Out of scope**: `WaitAndRetryAsync(int, Func<int, TimeSpan>)` (that is `polly.wait-and-retry-async`; `Find` splits by overload,
so one call never belongs to both), Context- or outcome-taking sleep-duration providers (Manual `OverloadNotSupported`).

## Requirements

- **REQ-1**: WHEN `RetryAsync(n)`/`RetryAsync()` THE SYSTEM SHALL emit `MaxRetryAttempts = n`/`1` and `Delay = TimeSpan.Zero`
  (v7 retries at once; proofs `retry-count`, `retry-default`).
- **REQ-2**: WHEN `RetryForeverAsync`/`WaitAndRetryForeverAsync` THE SYSTEM SHALL emit `MaxRetryAttempts = int.MaxValue` (v8's
  documented "retry indefinitely"), the provider as `DelayGenerator` with `AttemptNumber + 1` (proof `wait-forever`).
- **REQ-3**: WHEN `WaitAndRetryAsync(durations)` with `new[] { ... }`, `new TimeSpan[] { ... }` or `[ ... ]` of `TimeSpan.Zero` /
  `TimeSpan.FromX(literal)` THE SYSTEM SHALL emit `MaxRetryAttempts = <count>` and a `DelayGenerator` switching on
  `AttemptNumber` (proof `durations`); otherwise Manual `SleepDurationsNotLiteral` (proof `durations-variable`).
- **REQ-4**: THE SYSTEM SHALL map `onRetry`, including `Action<Exception, int>` of `RetryAsync`, like `polly.wait-and-retry-async` REQ-13/14.
- **REQ-5**: THE SYSTEM SHALL give equal run-time traces before (Polly 7.2.4) and after (Polly.Core) for every Fixed proof (`PollyBehaviourProofTests`).
- **REQ-6**: WHEN the retry count is not an integer literal ≥ 1 THE SYSTEM SHALL report Manual `RetryCountNotLiteral`.

## Assumptions

- v8 `Delay = TimeSpan.Zero` with the default constant backoff and no jitter waits nothing, like v7 `Retry`.
- An endless retry cannot be run to the end: the proof pins equal behaviour up to success after several attempts.

## Models

[polly-retry-variants-modellen.md](polly-retry-variants-modellen.md).

## Open questions

- None.
