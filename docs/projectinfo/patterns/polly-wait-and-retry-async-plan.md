---
kingmadoc: 1
feature: "polly-wait-and-retry-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/WaitAndRetryAsyncPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Core/Fixes/Polly/PollyFix.cs
    - src/Unshackle.Tests/Proofs/Polly/WaitAndRetryAsync/
---

# Feature: polly.wait-and-retry-async: WaitAndRetryAsync + ExecuteAsync → ResiliencePipeline with AddRetry

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308 step 4, queue row 1 (`claude/tasks/polly-queue.md`), council round 8 (general rule, P1, P2).

## One-sentence summary

polly.wait-and-retry-async: `Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay)` held in a local (or inline) and run only
through awaited `ExecuteAsync(() => ...)` calls becomes a `new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions { ... }).Build()`
pipeline, with the delay lambda kept as the `DelayGenerator`.

## Scope (in / out)

**In scope**

- `WaitAndRetryAsync(int, Func<int, TimeSpan>)` from assembly `Polly` (Polly 7, and Polly 8 still on the v7 API: council P5) on a
  receiver `Policy.Handle<E>()` without a predicate.
- The policy inline (`await Policy...WaitAndRetryAsync(...).ExecuteAsync(...)`) or in a local (`var` or an explicit type).
- `ExecuteAsync(Func<Task>)` and `ExecuteAsync(Func<Task<T>>)` with a parameterless lambda, directly awaited (also via `.ConfigureAwait(...)`).

**Out of scope (Manual, typed reason code, later queue rows)**

- `Or<E>()`, `HandleResult`, `HandleInner`, `Handle<E>(predicate)` (row 2), `onRetry` and other overloads (row 3), fields,
  properties, return values and arguments holding the policy (row 5), sync `WaitAndRetry`/`Execute` (row 4).

## Requirements

- **REQ-1**: WHEN the shape is in scope THE SYSTEM SHALL emit `ShouldHandle = new PredicateBuilder().Handle<E>()`,
  `MaxRetryAttempts = n` and `.Build()` (proofs `local-block-delay`, `inline-result`, `typed-local-not-handled`).
- **REQ-2**: THE SYSTEM SHALL keep the delay lambda body verbatim in a local function `TimeSpan SleepDuration(int attempt)` called with
  `args.AttemptNumber + 1`, because v7 passes the 1-based retry number and v8 the 0-based attempt (council P2). The behaviour proof
  fails when the `+ 1` is dropped (`PollyBehaviourProofTests.Proof_WithoutAttemptOffset_DifferentTrace`).
- **REQ-3**: WHEN the callback is `async () => ...` THE SYSTEM SHALL write `async _ => ...`; WHEN it is `() => expr` with `expr` a
  `Task`/`Task<T>` THE SYSTEM SHALL write `_ => new ValueTask(expr)` / `_ => new ValueTask<T>(expr)` (no added await, so no captured context).
- **REQ-4**: WHEN the local has an explicit type THE SYSTEM SHALL change it to `ResiliencePipeline` (proof `typed-local-not-handled`).
- **REQ-5**: THE SYSTEM SHALL produce the same run-time trace before (Polly 7.2.4) and after (Polly.Core via Microsoft.Extensions.Resilience
  10.11.0): attempts, delay arguments, final exception or result, and an unhandled exception passing through on the first call
  (`PollyBehaviourProofTests`).
- **REQ-6**: WHEN the retry count is not an integer literal of at least 1 THE SYSTEM SHALL report Manual `RetryCountNotLiteral`
  (v8 rejects `MaxRetryAttempts = 0`; proof `retry-count-variable`).
- **REQ-7**: WHEN any use of the policy local is not an awaited, rewritable `ExecuteAsync` THE SYSTEM SHALL report the declaration and
  every call Manual with the same reason (all or none per local; proof `result-not-awaited`, unit tests).
- **REQ-8**: WHEN the policy is stored or passed on outside a local THE SYSTEM SHALL report Manual `PolicyNotLocal` (proof `policy-returned`).

## Assumptions

- v8 `RetryStrategyOptions`: a non-null `DelayGenerator` result replaces `Delay`/`BackoffType`/`UseJitter`; `PredicateBuilder.Handle<E>()`
  matches `E` and derived types like v7 `Handle<E>()`. Pinned by the behaviour proofs, not only by reading.
- v7 and v8 both run the callback without the captured context and rethrow the last exception when retries run out.

## Risks

- `args` and `SleepDuration` are new names in the user's scope: a body already using either is Manual `NameClash`.

## Models

Class diagram, sequence and decision tree: [polly-wait-and-retry-async-modellen.md](polly-wait-and-retry-async-modellen.md).

## Open questions

- None for this row; package-level questions are in TAAK-308.
