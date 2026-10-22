---
kingmadoc: 1
feature: "polly-timeout"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/TimeoutPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/Timeout/
---

# Feature: polly.timeout: Policy.Timeout(Async) + Execute(Async) → AddTimeout

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 9 (`claude/tasks/polly-queue.md`), council round 8 general rule.

## One-sentence summary

polly.timeout: an optimistic v7 timeout with a literal duration, held in a local, a private field or inline, becomes `new ResiliencePipelineBuilder(<T>)().AddTimeout(timeout).Build()`; `Execute`/`ExecuteAsync(async ct => ..., token)` keep their shape.

## Scope (in / out)

**In scope**: `Timeout(TimeSpan)`, `Timeout(int seconds)`, `Timeout(x, TimeoutStrategy.Optimistic)`, generic `Timeout<T>`, sync and async; `ExecuteAsync(async ct => ..., token)` and the parameterless forms of the retry patterns.

**Out of scope**: pessimistic timeouts, timeout providers, `onTimeout` callbacks, durations outside 100 ms..1 day or not literal, a non-async token lambda, timeouts inside a wrap (wrap stays `WrapOrderNotProvable`).

## Requirements

- **REQ-1**: WHEN the timeout is a literal between 100 ms and 1 day and optimistic THE SYSTEM SHALL emit `AddTimeout(<same TimeSpan>)`, `int` seconds as `TimeSpan.FromSeconds(n)` (proofs `token-honoured`, `token-ignored`, `int-seconds`, `sync`, `typed`).
- **REQ-2**: THE SYSTEM SHALL give equal run-time traces before (Polly 7.2.4) and after (Polly.Core): a callback honouring the token ends in `TimeoutRejectedException` in both, one ignoring it completes in both (`PollyBehaviourProofTests`).
- **REQ-3**: WHEN the strategy is pessimistic THE SYSTEM SHALL report Manual `TimeoutStrategyNotOptimistic` (proof `pessimistic`).
- **REQ-4**: WHEN the timeout is not a literal or lies outside 100 ms..1 day (probe: 5 ms and 30 days fail v8 validation at `Build()`) THE SYSTEM SHALL report Manual `TimeoutNotProvable` (proofs `too-small`, `variable`; unit tests with overflowing and negative literals).
- **REQ-5**: WHEN a provider or `onTimeout` callback is passed THE SYSTEM SHALL report Manual `OverloadNotSupported` / `TimeoutNotProvable` (proof `on-timeout`; unit test).
- **REQ-6**: WHEN the token callback is not an async lambda THE SYSTEM SHALL report Manual `ExecuteNotLambda` (proof `token-sync-lambda`).

## Assumptions

- v7 optimistic and v8 timeouts both only cancel the token handed to the callback; the probe measured equal results for honoured, ignored, thrown-inside, int seconds, sync and typed cases. The 100 ms..1 day range is what the probe saw accepted (5 ms, 0 and 30 days were rejected by v8); it is conservative, not v8's exact bounds.

## Models

[polly-timeout-modellen.md](polly-timeout-modellen.md).

## Open questions

- None.
